namespace Drive.Infrastructure.Storage.S3;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; set; } = string.Empty;

    public string ServiceUrl { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;
}