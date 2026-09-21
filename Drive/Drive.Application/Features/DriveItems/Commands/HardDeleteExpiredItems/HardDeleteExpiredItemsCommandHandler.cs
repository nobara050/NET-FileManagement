using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Models;
using Drive.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drive.Application.Features.DriveItems.Commands.HardDeleteExpiredItems;

public sealed class HardDeleteExpiredItemsCommandHandler : IRequestHandler<HardDeleteExpiredItemsCommand, int>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IFileStorage _fileStorage;
    private readonly TrashSettings _trashSettings;
    private readonly ILogger<HardDeleteExpiredItemsCommandHandler> _logger;

    public HardDeleteExpiredItemsCommandHandler(
        IDriveItemRepository driveItemRepository,
        IFileStorage fileStorage,
        IOptions<TrashSettings> trashSettings,
        ILogger<HardDeleteExpiredItemsCommandHandler> logger)
    {
        _driveItemRepository = driveItemRepository;
        _fileStorage = fileStorage;
        _trashSettings = trashSettings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(
        HardDeleteExpiredItemsCommand request,
        CancellationToken cancellationToken)
    {
        var cutoff = request.CutoffTime ?? DateTimeOffset.UtcNow.AddDays(-_trashSettings.RetentionDays);

        var expiredItems = await _driveItemRepository.GetExpiredDeletedItemsAsync(
            cutoff,
            cancellationToken);

        if (expiredItems.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation(
            "Found {Count} expired items deleted before {Cutoff} for hard deletion.",
            expiredItems.Count,
            cutoff);

        // Clean up binary file versions in S3 for files
        foreach (var item in expiredItems)
        {
            if (item.ItemType == DriveItemType.File && item.Versions.Count > 0)
            {
                foreach (var version in item.Versions)
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(version.S3ObjectKey, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to delete S3 object {ObjectKey} for FileVersion {VersionId}",
                            version.S3ObjectKey,
                            version.Id);
                    }
                }
            }
        }

        // Delete from database using topological leaf-first deletion
        await _driveItemRepository.HardDeleteItemsAsync(expiredItems, cancellationToken);

        _logger.LogInformation("Successfully hard deleted {Count} expired items.", expiredItems.Count);

        return expiredItems.Count;
    }
}
