namespace Drive.Application.Common.Models;

public class TrashSettings
{
    public const string SectionName = "TrashSettings";

    public int RetentionDays { get; set; } = 30;

    public int CleanupIntervalHours { get; set; } = 24;
}
