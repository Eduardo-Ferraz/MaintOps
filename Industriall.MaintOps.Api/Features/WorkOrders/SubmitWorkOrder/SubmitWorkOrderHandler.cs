using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Infrastructure.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Industriall.MaintOps.Api.Features.WorkOrders.SubmitWorkOrder;

internal sealed class SubmitWorkOrderHandler
    : IRequestHandler<SubmitWorkOrderCommand, SubmitWorkOrderResponse>
{
    private readonly ApplicationDbContext _db;

    public SubmitWorkOrderHandler(ApplicationDbContext db) => _db = db;

    public async Task<SubmitWorkOrderResponse> Handle(
        SubmitWorkOrderCommand command,
        CancellationToken      cancellationToken)
    {
        // 1. Verify the equipment exists and is active.
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == command.EquipmentId && e.IsActive, cancellationToken);

        if (!equipmentExists)
            throw new NotFoundException(nameof(Equipment), command.EquipmentId);

        // 2. Create the domain aggregate – enforces invariants via Result pattern.
        var result = WorkOrder.Create(command.EquipmentId, command.Description);

        if (result.IsFailure)
            throw new DomainException(result.Error);

        var workOrder = result.Value!;

        // 3. Persist.
        _db.WorkOrders.Add(workOrder);
        await _db.SaveChangesAsync(cancellationToken);

        // 4. Project to response DTO.
        return new SubmitWorkOrderResponse(
            workOrder.Id,
            workOrder.EquipmentId,
            workOrder.Description,
            workOrder.Status.ToString());
    }
}
