namespace Drive.Api.Features.DriveItems.CreateFolder;

public sealed class CreateFolderRequest
{
    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;
}