namespace Industriall.MaintOps.Api.Domain.ValueObjects;

/// <summary>
/// Value object that encapsulates a maintenance time window.
/// Invariant: StartDate must be strictly before EndDate.
/// </summary>
public sealed class MaintenanceSchedule
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate   { get; private set; }

    // Required by EF Core owned-entity materialisation.
    private MaintenanceSchedule() { }

    /// <summary>
    /// Factory method. Returns a Failure result if the invariant is violated.
    /// </summary>
    public static Result<MaintenanceSchedule> Create(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return Result<MaintenanceSchedule>.Failure(
                "Schedule StartDate must be strictly before EndDate.");

        if (startDate < DateTime.UtcNow.Date)
            return Result<MaintenanceSchedule>.Failure(
                "Schedule StartDate cannot be in the past.");

        return Result<MaintenanceSchedule>.Success(new MaintenanceSchedule
        {
            StartDate = startDate,
            EndDate   = endDate
        });
    }

    /// <summary>
    /// Returns true when this schedule temporally overlaps another.
    /// Uses the standard half-open interval overlap check.
    /// </summary>
    public bool OverlapsWith(MaintenanceSchedule other)
        => StartDate < other.EndDate && EndDate > other.StartDate;

    public override string ToString()
        => $"[{StartDate:yyyy-MM-dd HH:mm} – {EndDate:yyyy-MM-dd HH:mm}]";

    public override bool Equals(object? obj)
        => obj is MaintenanceSchedule other
           && StartDate == other.StartDate
           && EndDate   == other.EndDate;

    public override int GetHashCode() => HashCode.Combine(StartDate, EndDate);
}
