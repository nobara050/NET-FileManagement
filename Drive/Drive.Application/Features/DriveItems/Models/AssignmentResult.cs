namespace Drive.Application.Features.DriveItems.Models;

public sealed class AssignmentResult
{
    public Guid UserId { get; init; }

    public string? UserEmail { get; init; }

    public string? DisplayName { get; init; }

    public Guid RoleId { get; init; }

    public string RoleName { get; init; } = null!;

    public bool IsDirect { get; init; }

    /// <summary>
    /// For inherited assignments, the ID of the ancestor item from which
    /// this permission was propagated. Null for direct assignments.
    /// </summary>
    public Guid? SourceItemId { get; init; }
}
