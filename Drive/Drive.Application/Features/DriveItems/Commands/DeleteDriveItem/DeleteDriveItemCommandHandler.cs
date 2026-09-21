using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.DeleteDriveItem;

public sealed class DeleteDriveItemCommandHandler : IRequestHandler<DeleteDriveItemCommand, bool>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public DeleteDriveItemCommandHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IPermissionService permissionService)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<bool> Handle(DeleteDriveItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var item = await _driveItemRepository.GetByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || item.IsDeleted)
        {
            throw new NotFoundException("Drive item not found.");
        }

        // Caller must own the item, own the parent folder, or have drive.delete permission.
        if (item.OwnerId != userId.Value)
        {
            var isFolderOwner = false;
            if (item.ParentId.HasValue)
            {
                var parent = await _driveItemRepository.GetByIdAsync(item.ParentId.Value, cancellationToken);
                if (parent is not null && parent.OwnerId == userId.Value)
                {
                    isFolderOwner = true;
                }
            }

            if (!isFolderOwner)
            {
                var hasDelete = await _permissionService.HasPermissionAsync(
                    userId.Value,
                    item.Id,
                    Common.Authorization.Permissions.DriveDelete,
                    cancellationToken);

                if (!hasDelete)
                {
                    throw new ForbiddenException("You do not have permission to delete this drive item.");
                }
            }
        }

        var now = DateTimeOffset.UtcNow;

        if (item.ItemType == DriveItemType.Folder)
        {
            // If the deleted drive item is a folder, delete all the things inside the folder too.
            var descendants = await _driveItemRepository.GetActiveDescendantsAsync(
                item.Id,
                cancellationToken);

            foreach (var descendant in descendants)
            {
                descendant.IsDeleted = true;
                descendant.DeletedAt = now;
                descendant.UpdatedAt = now;
                _driveItemRepository.Update(descendant);
            }
        }

        item.IsDeleted = true;
        item.DeletedAt = now;
        item.UpdatedAt = now;

        _driveItemRepository.Update(item);

        await _driveItemRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
