namespace Drive.Application.Features.DriveItems.Models;

public sealed class FileUpload
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long Length { get; init; }

    public Stream Content { get; init; } = null!;
}