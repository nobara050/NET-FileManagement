namespace Drive.Application.Common.Interfaces;

public interface IFileStorage
{
    string BucketName { get; }

    Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}