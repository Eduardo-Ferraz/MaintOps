namespace Industriall.MaintOps.Api.Features.WorkOrders.CompleteWorkOrder;

/// <summary>
/// Command to transition a WorkOrder to Completed status.
/// Domain Rule 1: Cannot complete a Pending WorkOrder.
/// </summary>
public sealed record CompleteWorkOrderCommand(Guid Id) : IRequest<CompleteWorkOrderResponse>;

public sealed record CompleteWorkOrderResponse(Guid Id, string Status);
