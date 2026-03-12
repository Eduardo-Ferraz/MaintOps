namespace Industriall.MaintOps.Api.Domain.Entities;

/// <summary>
/// Represents a physical or logical piece of industrial equipment.
/// </summary>
public sealed class Equipment
{
    public Guid           Id          { get; private set; }
    public string         Name        { get; private set; } = default!;
    public CriticalityLevel Criticality { get; private set; }
    public bool           IsActive    { get; private set; }

    // Required by EF Core.
    private Equipment() { }

    /// <summary>
    /// Creates a new active Equipment entity.
    /// </summary>
    public static Result<Equipment> Create(string name, CriticalityLevel criticality)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Equipment>.Failure("Equipment name cannot be empty.");

        if (name.Length > 200)
            return Result<Equipment>.Failure("Equipment name cannot exceed 200 characters.");

        return Result<Equipment>.Success(new Equipment
        {
            Id          = Guid.NewGuid(),
            Name        = name.Trim(),
            Criticality = criticality,
            IsActive    = true
        });
    }

    /// <summary>Marks the equipment as active so it can accept work orders.</summary>
    public void Activate()   => IsActive = true;

    /// <summary>Deactivates the equipment; work orders cannot be assigned to inactive equipment.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Updates the criticality level of the equipment.</summary>
    public Result UpdateCriticality(CriticalityLevel newLevel)
    {
        if (Criticality == newLevel)
            return Result.Failure($"Equipment is already at criticality level '{newLevel}'.");

        Criticality = newLevel;
        return Result.Success();
    }
}
