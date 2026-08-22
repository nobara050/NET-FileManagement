using Drive.Domain.Enums;

namespace Drive.Api.Features.DriveItems.List;

public sealed class DriveItemResponse
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DriveItemType ItemType { get; set; }

    public string? MimeType { get; set; }

    public long? Size { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}