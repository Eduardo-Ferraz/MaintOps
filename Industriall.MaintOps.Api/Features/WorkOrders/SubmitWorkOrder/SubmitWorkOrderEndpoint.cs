using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Industriall.MaintOps.Api.Features.WorkOrders.SubmitWorkOrder;

/// <summary>
/// Carter module – thin routing only, no business logic.
/// POST /work-orders
/// </summary>
public sealed class SubmitWorkOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/work-orders", async (
            SubmitWorkOrderCommand command,
            ISender                sender,
            CancellationToken      ct) =>
        {
            var response = await sender.Send(command, ct);
            return Results.CreatedAtRoute("GetWorkOrderById", new { id = response.Id }, response);
        })
        .WithName("SubmitWorkOrder")
        .WithTags("WorkOrders")
        .RequireRateLimiting("fixed")
        .RequireAuthorization();
    }
}
