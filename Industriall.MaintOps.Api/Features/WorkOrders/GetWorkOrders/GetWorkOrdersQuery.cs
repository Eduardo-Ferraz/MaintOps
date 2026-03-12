namespace Industriall.MaintOps.Api.Features.WorkOrders.GetWorkOrders;

/// <summary>
/// Query with optional filters and mandatory pagination.
/// </summary>
public sealed record GetWorkOrdersQuery(
    Guid?            EquipmentId   = null,
    WorkOrderStatus? Status        = null,
    string?          DescriptionContains = null,
    int              Page          = 1,
    int              PageSize      = 20
) : IRequest<PagedResult<WorkOrderSummary>>;

public sealed record WorkOrderSummary(
    Guid     Id,
    Guid     EquipmentId,
    string   Description,
    string   Status,
    DateTime? ScheduleStartDate,
    DateTime? ScheduleEndDate
);

/// <summary>
/// Generic pagination envelope returned by all list endpoints.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalCount,
    int              Page,
    int              PageSize,
    int              TotalPages
);
