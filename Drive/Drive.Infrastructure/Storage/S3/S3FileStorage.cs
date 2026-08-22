using Amazon.S3;
using Amazon.S3.Model;
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

    public async Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
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
        await _s3.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = objectKey
            },
            cancellationToken);
    }
}