namespace Drive.Domain.Entities;

public class DriveItemRoleAssignment
{
    public Guid Id { get; set; }

    public Guid DriveItemId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? SourceItemId { get; set; }

    public bool IsDirect { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}