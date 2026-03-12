namespace Industriall.MaintOps.Api.Features.WorkOrders.SubmitWorkOrder;

/// <summary>
/// Command to create a new WorkOrder in Pending status.
/// </summary>
public sealed record SubmitWorkOrderCommand(
    Guid   EquipmentId,
    string Description
) : IRequest<SubmitWorkOrderResponse>;

public sealed record SubmitWorkOrderResponse(
    Guid   Id,
    Guid   EquipmentId,
    string Description,
    string Status
);
