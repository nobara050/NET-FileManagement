using Drive.Domain.Enums;

namespace Drive.Domain.Entities;

public class DriveItem
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public DriveItemType ItemType { get; set; }

    public string? MimeType { get; set; }

    public long? Size { get; set; }

    public string? Checksum { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DriveItem? Parent { get; set; }

    public ICollection<DriveItem> Children { get; set; } = new List<DriveItem>();

    public ICollection<FileVersion> Versions { get; set; }
    = new List<FileVersion>();
}