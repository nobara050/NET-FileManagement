namespace Drive.Api.Features.DriveItems.Download;

public sealed class DownloadUrlResponse
{
    public string Url { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
