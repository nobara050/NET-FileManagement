using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Drive.Application.Features.DriveItems.Commands.HardDeleteDriveItem;

public sealed class HardDeleteDriveItemCommandHandler : IRequestHandler<HardDeleteDriveItemCommand, bool>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<HardDeleteDriveItemCommandHandler> _logger;

    public HardDeleteDriveItemCommandHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<HardDeleteDriveItemCommandHandler> logger)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<bool> Handle(
        HardDeleteDriveItemCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return false;
        }

        var item = await _driveItemRepository.GetDeletedByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || !item.IsDeleted)
        {
            return false;
        }

        // Caller must own the item or own the parent folder.
        if (item.OwnerId != userId.Value)
        {
            var isFolderOwner = false;
            if (item.ParentId.HasValue)
            {
                var parent = await _driveItemRepository.GetByIdAsync(item.ParentId.Value, cancellationToken);
                if (parent is null)
                {
                    parent = await _driveItemRepository.GetDeletedByIdAsync(item.ParentId.Value, cancellationToken);
                }

                if (parent is not null && parent.OwnerId == userId.Value)
                {
                    isFolderOwner = true;
                }
            }

            if (!isFolderOwner)
            {
                return false;
            }
        }

        var itemsToDelete = new List<DriveItem> { item };

        if (item.ItemType == DriveItemType.Folder)
        {
            var descendants = await _driveItemRepository.GetDeletedDescendantsAsync(
                item.Id,
                cancellationToken);

            itemsToDelete.AddRange(descendants);
        }

        // Delete S3 binaries for all files in the deletion set
        foreach (var fileItem in itemsToDelete.Where(x => x.ItemType == DriveItemType.File))
        {
            foreach (var version in fileItem.Versions)
            {
                await _fileStorage.DeleteAsync(version.S3ObjectKey, cancellationToken);
            }
        }

        await _driveItemRepository.HardDeleteItemsAsync(itemsToDelete, cancellationToken);

        return true;
    }
}
