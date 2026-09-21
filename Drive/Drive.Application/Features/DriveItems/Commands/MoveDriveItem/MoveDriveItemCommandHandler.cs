using AutoMapper;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.MoveDriveItem;

public sealed class MoveDriveItemCommandHandler
    : IRequestHandler<MoveDriveItemCommand, DriveItemResult>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IMapper _mapper;

    public MoveDriveItemCommandHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _mapper = mapper;
    }

    public async Task<DriveItemResult> Handle(
        MoveDriveItemCommand request,
        CancellationToken cancellationToken)
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

        // Caller must be owner or have drive.move permission on the item
        if (item.OwnerId != userId.Value)
        {
            var hasMove = await _permissionService.HasPermissionAsync(
                userId.Value,
                item.Id,
                Common.Authorization.Permissions.DriveMove,
                cancellationToken);

            if (!hasMove)
            {
                throw new ForbiddenException("You do not have permission to move this item.");
            }
        }

        if (item.ParentId == request.TargetParentId)
        {
            return _mapper.Map<DriveItemResult>(item);
        }

        DriveItem? targetParent = null;
        if (request.TargetParentId.HasValue)
        {
            if (request.TargetParentId.Value == item.Id)
            {
                throw new BadRequestException("Cannot move an item into itself.");
            }

            targetParent = await _driveItemRepository.GetByIdAsync(
                request.TargetParentId.Value,
                cancellationToken);

            if (targetParent is null || targetParent.IsDeleted)
            {
                throw new NotFoundException("Destination folder not found.");
            }

            if (targetParent.ItemType != DriveItemType.Folder)
            {
                throw new BadRequestException("Destination must be a folder.");
            }

            // Destination folder must be owned by the caller or caller has drive.create permission
            if (targetParent.OwnerId != userId.Value)
            {
                var hasTargetPermission = await _permissionService.HasPermissionAsync(
                    userId.Value,
                    targetParent.Id,
                    Common.Authorization.Permissions.DriveCreate,
                    cancellationToken);

                if (!hasTargetPermission)
                {
                    throw new ForbiddenException("You do not have permission to move items into the destination folder.");
                }
            }

            // If the item is a folder, it cannot be moved into any of its own descendants
            if (item.ItemType == DriveItemType.Folder)
            {
                var descendants = await _driveItemRepository.GetActiveDescendantsAsync(
                    item.Id,
                    cancellationToken);

                if (descendants.Any(d => d.Id == request.TargetParentId.Value))
                {
                    throw new BadRequestException("Cannot move a folder into one of its descendants.");
                }
            }
        }

        // Check for duplicate name in destination
        var duplicateExists = await _driveItemRepository.IsDuplicateNameAsync(
            userId.Value,
            request.TargetParentId,
            item.Name,
            item.Id,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"An item with name '{item.Name}' already exists in the destination folder.");
        }

        var now = DateTimeOffset.UtcNow;

        // Collect all items in subtree (the item itself + all active descendants)
        var subtree = new List<DriveItem> { item };
        if (item.ItemType == DriveItemType.Folder)
        {
            var descendants = await _driveItemRepository.GetActiveDescendantsAsync(
                item.Id,
                cancellationToken);

            subtree.AddRange(descendants);
        }

        var subtreeIds = subtree.Select(x => x.Id).ToList();

        // 1. Remove all old inherited role assignments on the subtree (keeping direct roles)
        await _assignmentRepository.RemoveInheritedByItemIdsAsync(subtreeIds, cancellationToken);

        // 2. If moving to a target folder, inherit the new parent's active role assignments
        if (targetParent is not null)
        {
            var parentAssignments = await _assignmentRepository.ListByDriveItemIdAsync(
                targetParent.Id,
                cancellationToken);

            if (parentAssignments is not null)
            {
                foreach (var subItem in subtree)
                {
                    foreach (var pa in parentAssignments)
                    {
                        if (pa.UserId == userId.Value)
                        {
                            continue;
                        }

                        // Explicit roles on the item/descendants are preserved and take precedence
                        var hasExplicit = await _assignmentRepository.ExistsDirectAssignmentAsync(
                            subItem.Id,
                            pa.UserId,
                            cancellationToken);

                        if (hasExplicit)
                        {
                            continue;
                        }

                        await _assignmentRepository.AddAsync(
                            new DriveItemRoleAssignment
                            {
                                Id = Guid.NewGuid(),
                                DriveItemId = subItem.Id,
                                UserId = pa.UserId,
                                RoleId = pa.RoleId,
                                SourceItemId = pa.SourceItemId ?? targetParent.Id,
                                IsDirect = false,
                                CreatedBy = pa.CreatedBy,
                                CreatedAt = now
                            },
                            cancellationToken);
                    }
                }
            }
        }

        // 3. Update ParentId and UpdatedAt on the moved item
        item.ParentId = request.TargetParentId;
        item.UpdatedAt = now;

        _driveItemRepository.Update(item);
        await _driveItemRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DriveItemResult>(item);
    }
}
