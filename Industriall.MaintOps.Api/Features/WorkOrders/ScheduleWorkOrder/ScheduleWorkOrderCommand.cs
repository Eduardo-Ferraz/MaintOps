namespace Industriall.MaintOps.Api.Features.WorkOrders.ScheduleWorkOrder;

/// <summary>
/// Command to assign a MaintenanceSchedule to an existing WorkOrder.
/// Enforces Domain Rule 3: blocks scheduling if the equipment already has a
/// High-criticality WorkOrder with overlapping dates.
/// </summary>
public sealed record ScheduleWorkOrderCommand(
    Guid     Id,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<ScheduleWorkOrderResponse>;

public sealed record ScheduleWorkOrderResponse(
    Guid     Id,
    string   Status,
    DateTime ScheduleStartDate,
    DateTime ScheduleEndDate
);
