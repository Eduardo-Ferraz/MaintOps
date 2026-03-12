using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Industriall.MaintOps.Api.Features.WorkOrders.CompleteWorkOrder;

/// <summary>
/// PATCH /work-orders/{id}/complete
/// </summary>
public sealed class CompleteWorkOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/work-orders/{id:guid}/complete", async (
            Guid              id,
            ISender           sender,
            CancellationToken ct) =>
        {
            var response = await sender.Send(new CompleteWorkOrderCommand(id), ct);
            return Results.Ok(response);
        })
        .WithName("CompleteWorkOrder")
        .WithTags("WorkOrders")
        .RequireRateLimiting("fixed")
        .RequireAuthorization();
    }
}
