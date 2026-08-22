namespace Drive.Api.Features.DriveItems.CreateFile;

public sealed class CreateFileRequest
{
    public Guid? ParentId { get; set; }

    public IFormFile File { get; set; } = null!;
}