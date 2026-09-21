namespace Drive.Application.Features.DriveItems.Models;

public sealed class DownloadUrlResult
{
    public string Url { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
