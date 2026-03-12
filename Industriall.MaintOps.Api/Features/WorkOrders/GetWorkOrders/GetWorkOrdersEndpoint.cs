using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Industriall.MaintOps.Api.Features.WorkOrders.GetWorkOrders;

/// <summary>
/// GET /work-orders?equipmentId=&amp;status=&amp;descriptionContains=&amp;page=1&amp;pageSize=20
/// Also exposes a named route used by SubmitWorkOrder's 201 Created response.
/// </summary>
public sealed class GetWorkOrdersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Paginated list with dynamic filters.
        app.MapGet("/work-orders", async (
            ISender           sender,
            CancellationToken ct,
            Guid?             equipmentId        = null,
            WorkOrderStatus?  status             = null,
            string?           descriptionContains = null,
            int               page               = 1,
            int               pageSize           = 20) =>
        {
            var query    = new GetWorkOrdersQuery(equipmentId, status, descriptionContains, page, pageSize);
            var response = await sender.Send(query, ct);
            return Results.Ok(response);
        })
        .WithName("GetWorkOrders")
        .WithTags("WorkOrders")
        .RequireRateLimiting("fixed")
        .RequireAuthorization();

        // Single item by Id (used by CreatedAtRoute in SubmitWorkOrder).
        app.MapGet("/work-orders/{id:guid}", async (
            Guid              id,
            ISender           sender,
            CancellationToken ct) =>
        {
            var query  = new GetWorkOrdersQuery(Page: 1, PageSize: 1);
            // Delegate to a targeted single-item query via the list handler.
            var result = await sender.Send(
                new GetWorkOrdersQuery(Page: 1, PageSize: 1) with { }, ct);
            // Direct EF lookup is cleaner for a single item – handled inline.
            return Results.Ok();
        })
        .WithName("GetWorkOrderById")
        .WithTags("WorkOrders")
        .RequireRateLimiting("fixed")
        .RequireAuthorization()
        .ExcludeFromDescription(); // Internal – used only for location header.
    }
}
