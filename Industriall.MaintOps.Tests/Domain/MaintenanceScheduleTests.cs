using FluentAssertions;
using Industriall.MaintOps.Api.Domain.ValueObjects;

namespace Industriall.MaintOps.Tests.Domain;

public sealed class MaintenanceScheduleTests
{
    // ── Domain Rule 2 ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_WhenStartDateBeforeEndDate_ShouldSucceed()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var end   = DateTime.UtcNow.AddDays(3);

        var result = MaintenanceSchedule.Create(start, end);

        result.IsSuccess.Should().BeTrue();
        result.Value!.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
    }

    [Fact]
    public void Create_WhenStartDateEqualsEndDate_ShouldFail()
    {
        var date = DateTime.UtcNow.AddDays(1);

        var result = MaintenanceSchedule.Create(date, date);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("StartDate must be strictly before EndDate");
    }

    [Fact]
    public void Create_WhenStartDateAfterEndDate_ShouldFail()
    {
        var result = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(2));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WhenStartDateIsInThePast_ShouldFail()
    {
        var result = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("past");
    }

    // ── Overlap detection ─────────────────────────────────────────────────────

    [Fact]
    public void OverlapsWith_WhenSchedulesOverlap_ShouldReturnTrue()
    {
        var s1 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5)).Value!;
        var s2 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(7)).Value!;

        s1.OverlapsWith(s2).Should().BeTrue();
        s2.OverlapsWith(s1).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenSchedulesAreContiguous_ShouldReturnFalse()
    {
        var s1 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3)).Value!;
        var s2 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(5)).Value!;

        // Half-open interval: end of s1 == start of s2 is NOT an overlap.
        s1.OverlapsWith(s2).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenSchedulesDoNotOverlap_ShouldReturnFalse()
    {
        var s1 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3)).Value!;
        var s2 = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(8)).Value!;

        s1.OverlapsWith(s2).Should().BeFalse();
    }
}
