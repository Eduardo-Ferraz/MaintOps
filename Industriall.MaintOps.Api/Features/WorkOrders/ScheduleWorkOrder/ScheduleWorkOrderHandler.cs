using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Industriall.MaintOps.Api.Features.WorkOrders.ScheduleWorkOrder;

internal sealed class ScheduleWorkOrderHandler
    : IRequestHandler<ScheduleWorkOrderCommand, ScheduleWorkOrderResponse>
{
    private readonly ApplicationDbContext _db;

    public ScheduleWorkOrderHandler(ApplicationDbContext db) => _db = db;

    public async Task<ScheduleWorkOrderResponse> Handle(
        ScheduleWorkOrderCommand command,
        CancellationToken        cancellationToken)
    {
        // 1. Load the work order.
        var workOrder = await _db.WorkOrders
            .FirstOrDefaultAsync(wo => wo.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), command.Id);

        // 2. Build the value object – enforces StartDate < EndDate (Domain Rule 2).
        var scheduleResult = MaintenanceSchedule.Create(command.StartDate, command.EndDate);
        if (scheduleResult.IsFailure)
            throw new DomainException(scheduleResult.Error);

        var schedule = scheduleResult.Value!;

        // 3. Domain Rule 3: if the equipment is High-criticality, block overlapping schedules.
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(e => e.Id == workOrder.EquipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Equipment), workOrder.EquipmentId);

        if (equipment.Criticality == CriticalityLevel.High)
        {
            var hasOverlap = await _db.WorkOrders
                .Where(wo =>
                    wo.Id            != command.Id                    &&
                    wo.EquipmentId   == workOrder.EquipmentId         &&
                    wo.Status        == WorkOrderStatus.Scheduled     &&
                    wo.Schedule      != null                          &&
                    wo.Schedule.StartDate < command.EndDate           &&
                    wo.Schedule.EndDate   > command.StartDate)
                .AnyAsync(cancellationToken);

            if (hasOverlap)
                throw new DomainException(
                    $"Equipment '{equipment.Name}' is High-criticality and already has a " +
                    $"WorkOrder scheduled within [{command.StartDate:yyyy-MM-dd} – {command.EndDate:yyyy-MM-dd}]. " +
                    "Overlapping schedules are not permitted.");
        }

        // 4. Apply domain state transition (Domain Rule: completed WO cannot be rescheduled).
        var assignResult = workOrder.AssignSchedule(schedule);
        if (assignResult.IsFailure)
            throw new DomainException(assignResult.Error);

        // 5. Persist.
        await _db.SaveChangesAsync(cancellationToken);

        return new ScheduleWorkOrderResponse(
            workOrder.Id,
            workOrder.Status.ToString(),
            schedule.StartDate,
            schedule.EndDate);
    }
}
