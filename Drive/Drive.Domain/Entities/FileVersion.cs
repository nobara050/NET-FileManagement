namespace Drive.Domain.Entities;

public class FileVersion
{
    public Guid Id { get; set; }

    public Guid DriveItemId { get; set; }

    public int VersionNumber { get; set; }

    public string S3Bucket { get; set; } = null!;

    public string S3ObjectKey { get; set; } = null!;

    public long Size { get; set; }

    public string? Checksum { get; set; }

    public string MimeType { get; set; } = null!;

    public bool IsCurrent { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DriveItem DriveItem { get; set; } = null!;
}