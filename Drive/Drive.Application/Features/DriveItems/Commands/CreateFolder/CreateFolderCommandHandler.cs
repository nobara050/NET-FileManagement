using AutoMapper;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFolder;

public sealed class CreateFolderCommandHandler
    : IRequestHandler<CreateFolderCommand, DriveItemResult>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IMapper _mapper;

    public CreateFolderCommandHandler(
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
        CreateFolderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var folderName = request.Name.Trim();

        if (request.ParentId.HasValue)
        {
            var parent = await _driveItemRepository.GetByIdAsync(
                request.ParentId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new NotFoundException("Parent folder not found.");
            }

            if (parent.OwnerId != userId.Value)
            {
                var hasCreate = await _permissionService.HasPermissionAsync(
                    userId.Value,
                    parent.Id,
                    Common.Authorization.Permissions.DriveCreate,
                    cancellationToken);

                if (!hasCreate)
                {
                    throw new ForbiddenException("You do not have permission to create folders inside this folder.");
                }
            }

            if (parent.ItemType != DriveItemType.Folder)
            {
                throw new BadRequestException("Target parent item must be a folder.");
            }
        }

        var duplicateExists = await _driveItemRepository.IsDuplicateNameAsync(
            userId.Value,
            request.ParentId,
            folderName,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"An item with name '{folderName}' already exists in this folder.");
        }

        var now = DateTimeOffset.UtcNow;

        var folder = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = userId.Value,
            ParentId = request.ParentId,
            Name = folderName,
            ItemType = DriveItemType.Folder,
            MimeType = null,
            Size = null,
            Checksum = null,
            IsDeleted = false,
            DeletedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _driveItemRepository.AddAsync(
            folder,
            cancellationToken);

        if (request.ParentId.HasValue)
        {
            var parentAssignments = await _assignmentRepository.ListByDriveItemIdAsync(
                request.ParentId.Value,
                cancellationToken);

            if (parentAssignments is not null)
            {
                foreach (var pa in parentAssignments)
                {
                    // Skip the owner — they already have implicit full access.
                    if (pa.UserId == userId.Value)
                    {
                        continue;
                    }

                    await _assignmentRepository.AddAsync(
                        new DriveItemRoleAssignment
                        {
                            Id = Guid.NewGuid(),
                            DriveItemId = folder.Id,
                            UserId = pa.UserId,
                            RoleId = pa.RoleId,
                            // Direct parent assignment → source is the parent.
                            // Inherited parent assignment → preserve the original source.
                            SourceItemId = pa.SourceItemId ?? request.ParentId.Value,
                            IsDirect = false,
                            CreatedBy = userId.Value,
                            CreatedAt = now
                        },
                        cancellationToken);
                }
            }
        }

        await _driveItemRepository.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<DriveItemResult>(folder);
    }
}