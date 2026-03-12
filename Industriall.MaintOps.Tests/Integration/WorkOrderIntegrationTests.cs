using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Industriall.MaintOps.Api.Domain.Entities;
using Industriall.MaintOps.Api.Domain.Enums;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Industriall.MaintOps.Tests.Integration;

/// <summary>
/// Integration tests that hit the actual HTTP endpoints with a real PostgreSQL database.
/// These tests validate the full vertical slice: Endpoint → Handler → Domain → DB.
/// </summary>
public sealed class WorkOrderIntegrationTests : IntegrationTestBase
{
    private async Task<Guid> SeedEquipmentAsync(
        CriticalityLevel criticality = CriticalityLevel.Medium,
        bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var equipment = Equipment.Create($"Equip-{Guid.NewGuid()}", criticality).Value!;
        if (!isActive) equipment.Deactivate();

        db.Equipment.Add(equipment);
        await db.SaveChangesAsync();
        return equipment.Id;
    }

    // ── POST /work-orders ─────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitWorkOrder_WithValidPayload_ShouldReturn201()
    {
        // Arrange
        var equipmentId = await SeedEquipmentAsync();
        var payload     = new { equipmentId, description = "Check motor bearings" };

        // Act
        var response = await Client.PostAsJsonAsync("/work-orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<dynamic>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitWorkOrder_WithNonExistentEquipment_ShouldReturn404()
    {
        var payload = new { equipmentId = Guid.NewGuid(), description = "Ghost equipment" };

        var response = await Client.PostAsJsonAsync("/work-orders", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitWorkOrder_WithEmptyDescription_ShouldReturn400()
    {
        var equipmentId = await SeedEquipmentAsync();
        var payload     = new { equipmentId, description = "" };

        var response = await Client.PostAsJsonAsync("/work-orders", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PATCH /work-orders/{id}/schedule ─────────────────────────────────────

    [Fact]
    public async Task ScheduleWorkOrder_HighCriticality_WhenOverlapExists_ShouldReturn422()
    {
        // Arrange – seed High-criticality equipment and two work orders.
        var equipmentId = await SeedEquipmentAsync(CriticalityLevel.High);

        var createPayload = new { equipmentId, description = "First job" };
        var createResp    = await Client.PostAsJsonAsync("/work-orders", createPayload);
        var firstWo       = await createResp.Content.ReadFromJsonAsync<dynamic>();
        Guid firstId      = firstWo!.GetProperty("id").GetGuid();

        var createPayload2 = new { equipmentId, description = "Second job" };
        var createResp2    = await Client.PostAsJsonAsync("/work-orders", createPayload2);
        var secondWo       = await createResp2.Content.ReadFromJsonAsync<dynamic>();
        Guid secondId      = secondWo!.GetProperty("id").GetGuid();

        // Schedule the first work order.
        var schedulePayload = new
        {
            startDate = DateTime.UtcNow.AddDays(1).ToString("o"),
            endDate   = DateTime.UtcNow.AddDays(5).ToString("o")
        };
        await Client.PatchAsJsonAsync($"/work-orders/{firstId}/schedule", schedulePayload);

        // Act – try to schedule second WO with overlapping dates.
        var overlapPayload = new
        {
            startDate = DateTime.UtcNow.AddDays(3).ToString("o"),
            endDate   = DateTime.UtcNow.AddDays(7).ToString("o")
        };
        var response = await Client.PatchAsJsonAsync(
            $"/work-orders/{secondId}/schedule", overlapPayload);

        // Assert – Domain Rule 3 → 422 Unprocessable Entity.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ScheduleWorkOrder_MediumCriticality_WhenOverlapExists_ShouldReturn200()
    {
        // Arrange – Medium criticality equipment does NOT trigger overlap check.
        var equipmentId = await SeedEquipmentAsync(CriticalityLevel.Medium);

        var wo1Resp = await Client.PostAsJsonAsync("/work-orders",
            new { equipmentId, description = "First" });
        Guid wo1Id = (await wo1Resp.Content.ReadFromJsonAsync<dynamic>())!.GetProperty("id").GetGuid();

        var wo2Resp = await Client.PostAsJsonAsync("/work-orders",
            new { equipmentId, description = "Second" });
        Guid wo2Id = (await wo2Resp.Content.ReadFromJsonAsync<dynamic>())!.GetProperty("id").GetGuid();

        await Client.PatchAsJsonAsync($"/work-orders/{wo1Id}/schedule", new
        {
            startDate = DateTime.UtcNow.AddDays(1).ToString("o"),
            endDate   = DateTime.UtcNow.AddDays(5).ToString("o")
        });

        // Act – overlapping dates on medium criticality should be allowed.
        var response = await Client.PatchAsJsonAsync($"/work-orders/{wo2Id}/schedule", new
        {
            startDate = DateTime.UtcNow.AddDays(3).ToString("o"),
            endDate   = DateTime.UtcNow.AddDays(7).ToString("o")
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── PATCH /work-orders/{id}/complete ─────────────────────────────────────

    [Fact]
    public async Task CompleteWorkOrder_WhenPending_ShouldReturn422()
    {
        var equipmentId = await SeedEquipmentAsync();
        var woResp      = await Client.PostAsJsonAsync("/work-orders",
            new { equipmentId, description = "Pending job" });
        Guid woId = (await woResp.Content.ReadFromJsonAsync<dynamic>())!.GetProperty("id").GetGuid();

        var response = await Client.PatchAsJsonAsync($"/work-orders/{woId}/complete", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── GET /work-orders ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkOrders_ShouldReturnPagedResult()
    {
        var equipmentId = await SeedEquipmentAsync();
        await Client.PostAsJsonAsync("/work-orders", new { equipmentId, description = "Task A" });
        await Client.PostAsJsonAsync("/work-orders", new { equipmentId, description = "Task B" });

        var response = await Client.GetAsync("/work-orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<dynamic>();
        body.Should().NotBeNull();
    }
}
