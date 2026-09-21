using AutoMapper;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RecoverDriveItem;

public sealed class RecoverDriveItemCommandHandler : IRequestHandler<RecoverDriveItemCommand, DriveItemResult>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public RecoverDriveItemCommandHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<DriveItemResult> Handle(
        RecoverDriveItemCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var item = await _driveItemRepository.GetDeletedByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || !item.IsDeleted)
        {
            throw new NotFoundException("Drive item not found in trash.");
        }

        // Only owner of that drive item can recover it.
        if (item.OwnerId != userId.Value)
        {
            throw new ForbiddenException("Only the owner of this drive item can recover it.");
        }

        // Verify parent folder status if item was inside a parent folder.
        if (item.ParentId.HasValue)
        {
            var parent = await _driveItemRepository.GetByIdAsync(
                item.ParentId.Value,
                cancellationToken);

            if (parent is null || parent.IsDeleted)
            {
                throw new ConflictException("Cannot recover item because its parent folder is deleted. Please recover the parent folder first.");
            }
        }

        // If the user recovers the item, check duplicate name first in the path they use.
        var hasDuplicate = await _driveItemRepository.IsDuplicateNameAsync(
            item.OwnerId,
            item.ParentId,
            item.Name,
            cancellationToken);

        if (hasDuplicate)
        {
            throw new ConflictException($"An active item with the name '{item.Name}' already exists in this location.");
        }

        var previousDeletedAt = item.DeletedAt;
        var now = DateTimeOffset.UtcNow;

        item.IsDeleted = false;
        item.DeletedAt = null;
        item.UpdatedAt = now;

        _driveItemRepository.Update(item);

        // If recovering a folder, also recover all things inside that were deleted with it.
        if (item.ItemType == DriveItemType.Folder && previousDeletedAt.HasValue)
        {
            var cascadedDescendants = await _driveItemRepository.GetCascadedDeletedDescendantsAsync(
                item.Id,
                previousDeletedAt.Value,
                cancellationToken);

            foreach (var descendant in cascadedDescendants)
            {
                descendant.IsDeleted = false;
                descendant.DeletedAt = null;
                descendant.UpdatedAt = now;
                _driveItemRepository.Update(descendant);
            }
        }

        await _driveItemRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DriveItemResult>(item);
    }
}
