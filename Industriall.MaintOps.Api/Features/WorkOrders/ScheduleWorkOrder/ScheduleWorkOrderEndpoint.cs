using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Industriall.MaintOps.Api.Features.WorkOrders.ScheduleWorkOrder;

/// <summary>
/// PATCH /work-orders/{id}/schedule
/// </summary>
public sealed class ScheduleWorkOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/work-orders/{id:guid}/schedule", async (
            Guid                      id,
            ScheduleWorkOrderRequest  request,
            ISender                   sender,
            CancellationToken         ct) =>
        {
            var command  = new ScheduleWorkOrderCommand(id, request.StartDate, request.EndDate);
            var response = await sender.Send(command, ct);
            return Results.Ok(response);
        })
        .WithName("ScheduleWorkOrder")
        .WithTags("WorkOrders")
        .RequireRateLimiting("fixed")
        .RequireAuthorization();
    }
}

/// <summary>Request body for the PATCH endpoint (Id comes from the route).</summary>
public sealed record ScheduleWorkOrderRequest(DateTime StartDate, DateTime EndDate);
