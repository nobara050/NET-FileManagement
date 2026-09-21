using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using Drive.Application.Common.Interfaces;

namespace Drive.Infrastructure.Storage.S3;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    
    private readonly S3Options _options;
    public string BucketName => _options.BucketName;

    public S3FileStorage(
        IAmazonS3 s3,
        IOptions<S3Options> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public string GetFileUrl(string objectKey)
    {
        return $"{_options.ServiceUrl.TrimEnd('/')}/{BucketName}/{objectKey}";
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.EnsureBucketExistsAsync", System.Diagnostics.ActivityKind.Internal);

        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, BucketName);
        if (!exists)
        {
            await _s3.PutBucketAsync(
                new PutBucketRequest
                {
                    BucketName = BucketName
                },
                cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.ExistsAsync", System.Diagnostics.ActivityKind.Internal);

        try
        {
            await _s3.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = BucketName,
                    Key = objectKey
                },
                cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.UploadAsync", System.Diagnostics.ActivityKind.Internal);

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(
            request,
            cancellationToken);
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.DeleteAsync", System.Diagnostics.ActivityKind.Internal);

        await _s3.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = objectKey
            },
            cancellationToken);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string objectKey,
        string fileName,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.GeneratePresignedDownloadUrlAsync", System.Diagnostics.ActivityKind.Internal);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"attachment; filename=\"{fileName}\"",
                ContentType = contentType
            }
        };

        var url = _s3.GetPreSignedURL(request);

        return Task.FromResult(url);
    }

    public Task<string> GeneratePresignedPreviewUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("FileStorage.GeneratePresignedPreviewUrlAsync", System.Diagnostics.ActivityKind.Internal);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                // No Content-Disposition → browser renders inline
                ContentType = contentType
            }
        };

        var url = _s3.GetPreSignedURL(request);

        return Task.FromResult(url);
    }
}