namespace Industriall.MaintOps.Api.Domain.Entities;

/// <summary>
/// Aggregate root that represents a maintenance work order.
/// All state mutations are performed through domain methods that enforce business rules.
/// </summary>
public sealed class WorkOrder
{
    public Guid                  Id          { get; private set; }
    public Guid                  EquipmentId { get; private set; }
    public string                Description { get; private set; } = default!;
    public WorkOrderStatus       Status      { get; private set; }

    /// <summary>
    /// The maintenance time window. Null until the work order is scheduled.
    /// </summary>
    public MaintenanceSchedule?  Schedule    { get; private set; }

    // Required by EF Core.
    private WorkOrder() { }

    // ── Factory ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new WorkOrder in Pending status.
    /// The caller is responsible for verifying that EquipmentId refers to an active equipment.
    /// </summary>
    public static Result<WorkOrder> Create(Guid equipmentId, string description)
    {
        if (equipmentId == Guid.Empty)
            return Result<WorkOrder>.Failure("EquipmentId must not be empty.");

        if (string.IsNullOrWhiteSpace(description))
            return Result<WorkOrder>.Failure("Description cannot be empty.");

        if (description.Length > 1000)
            return Result<WorkOrder>.Failure("Description cannot exceed 1000 characters.");

        return Result<WorkOrder>.Success(new WorkOrder
        {
            Id          = Guid.NewGuid(),
            EquipmentId = equipmentId,
            Description = description.Trim(),
            Status      = WorkOrderStatus.Pending
        });
    }

    // ── Domain Methods ─────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns a MaintenanceSchedule to this WorkOrder and transitions it to Scheduled.
    /// Domain Rule: A completed WorkOrder cannot be rescheduled.
    /// Note: The overlap check against other High-Criticality work orders is enforced
    /// at the application layer (handler) because it requires database access.
    /// </summary>
    public Result AssignSchedule(MaintenanceSchedule schedule)
    {
        if (Status == WorkOrderStatus.Completed)
            return Result.Failure(
                "A completed WorkOrder cannot be rescheduled.");

        Schedule = schedule;
        Status   = WorkOrderStatus.Scheduled;
        return Result.Success();
    }

    /// <summary>
    /// Transitions the WorkOrder to InProgress status.
    /// Domain Rule: Only a Scheduled WorkOrder can be started.
    /// </summary>
    public Result Start()
    {
        if (Status != WorkOrderStatus.Scheduled)
            return Result.Failure(
                $"Only a Scheduled WorkOrder can be started. Current status: '{Status}'.");

        Status = WorkOrderStatus.InProgress;
        return Result.Success();
    }

    /// <summary>
    /// Marks the WorkOrder as Completed.
    /// Domain Rule: A WorkOrder in Pending status cannot be completed directly.
    /// </summary>
    public Result Complete()
    {
        if (Status == WorkOrderStatus.Pending)
            return Result.Failure(
                "A WorkOrder in 'Pending' status cannot be completed. It must be scheduled first.");

        if (Status == WorkOrderStatus.Completed)
            return Result.Failure(
                "This WorkOrder has already been completed.");

        Status = WorkOrderStatus.Completed;
        return Result.Success();
    }
}
