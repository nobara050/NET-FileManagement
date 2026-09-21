namespace Drive.Application.Common.Interfaces;

public interface IFileStorage
{
    string BucketName { get; }

    string GetFileUrl(string objectKey);

    Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePresignedDownloadUrlAsync(
        string objectKey,
        string fileName,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL that serves the object inline (no forced download).
    /// Suitable for image, PDF, and text preview in the browser.
    /// </summary>
    Task<string> GeneratePresignedPreviewUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}