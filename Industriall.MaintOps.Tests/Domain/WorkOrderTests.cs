using FluentAssertions;
using Industriall.MaintOps.Api.Domain.Entities;
using Industriall.MaintOps.Api.Domain.Enums;
using Industriall.MaintOps.Api.Domain.ValueObjects;

namespace Industriall.MaintOps.Tests.Domain;

public sealed class WorkOrderTests
{
    private static readonly Guid ValidEquipmentId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSucceedAndBeInPendingStatus()
    {
        // Arrange / Act
        var result = WorkOrder.Create(ValidEquipmentId, "Replace hydraulic seal");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(WorkOrderStatus.Pending);
        result.Value.EquipmentId.Should().Be(ValidEquipmentId);
    }

    [Fact]
    public void Create_WithEmptyEquipmentId_ShouldFail()
    {
        var result = WorkOrder.Create(Guid.Empty, "Some description");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("EquipmentId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDescription_ShouldFail(string description)
    {
        var result = WorkOrder.Create(ValidEquipmentId, description);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Description");
    }

    // ── Domain Rule 1: Cannot complete a Pending WorkOrder ────────────────────

    [Fact]
    public void Complete_WhenStatusIsPending_ShouldFail()
    {
        // Arrange
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Inspect bearings").Value!;
        workOrder.Status.Should().Be(WorkOrderStatus.Pending);

        // Act
        var result = workOrder.Complete();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void Complete_WhenStatusIsScheduled_ShouldSucceed()
    {
        // Arrange
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Lube motor").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)).Value!;
        workOrder.AssignSchedule(schedule);

        // Act
        var result = workOrder.Complete();

        // Assert
        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public void Complete_WhenStatusIsInProgress_ShouldSucceed()
    {
        // Arrange
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Full overhaul").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)).Value!;
        workOrder.AssignSchedule(schedule);
        workOrder.Start();

        // Act
        var result = workOrder.Complete();

        // Assert
        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldFail()
    {
        // Arrange
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Calibrate sensor").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)).Value!;
        workOrder.AssignSchedule(schedule);
        workOrder.Complete();

        // Act
        var result = workOrder.Complete();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been completed");
    }

    // ── AssignSchedule ────────────────────────────────────────────────────────

    [Fact]
    public void AssignSchedule_WhenCompleted_ShouldFail()
    {
        // Arrange
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Replace belt").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)).Value!;
        workOrder.AssignSchedule(schedule);
        workOrder.Complete();

        // Act
        var result = workOrder.AssignSchedule(schedule);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("completed");
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_WhenNotScheduled_ShouldFail()
    {
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Pressure test").Value!;

        var result = workOrder.Start();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Scheduled");
    }

    [Fact]
    public void Start_WhenScheduled_ShouldTransitionToInProgress()
    {
        var workOrder = WorkOrder.Create(ValidEquipmentId, "Pressure test").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)).Value!;
        workOrder.AssignSchedule(schedule);

        var result = workOrder.Start();

        result.IsSuccess.Should().BeTrue();
        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
    }
}
