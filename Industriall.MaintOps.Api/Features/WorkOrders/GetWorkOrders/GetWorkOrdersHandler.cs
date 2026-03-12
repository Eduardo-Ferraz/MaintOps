using Industriall.MaintOps.Api.Infrastructure.Database;

namespace Industriall.MaintOps.Api.Features.WorkOrders.GetWorkOrders;

internal sealed class GetWorkOrdersHandler
    : IRequestHandler<GetWorkOrdersQuery, PagedResult<WorkOrderSummary>>
{
    private readonly ApplicationDbContext _db;

    public GetWorkOrdersHandler(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<WorkOrderSummary>> Handle(
        GetWorkOrdersQuery query,
        CancellationToken  cancellationToken)
    {
        return await _db.WorkOrders
            .AsNoTracking()
            .WithEquipment(query.EquipmentId)
            .WithStatus(query.Status)
            .WithDescriptionContaining(query.DescriptionContains)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
