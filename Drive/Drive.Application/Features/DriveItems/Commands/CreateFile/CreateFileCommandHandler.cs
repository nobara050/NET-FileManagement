using AutoMapper;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFile;

public sealed class CreateFileCommandHandler
    : IRequestHandler<CreateFileCommand, DriveItemResult>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly IRepository<FileVersion> _fileVersionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IFileStorage _fileStorage;
    private readonly IMapper _mapper;

    public CreateFileCommandHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        IRepository<FileVersion> fileVersionRepository,
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IFileStorage fileStorage,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _fileVersionRepository = fileVersionRepository;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _fileStorage = fileStorage;
        _mapper = mapper;
    }

    public async Task<DriveItemResult> Handle(
        CreateFileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var fileName = request.File.FileName.Trim();

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
                    throw new ForbiddenException("You do not have permission to add files to this folder.");
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
            fileName,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"An item with name '{fileName}' already exists in this folder.");
        }

        var driveItemId = Guid.NewGuid();
        var fileVersionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var objectKey = $"files/{driveItemId}/{fileVersionId}";

        var driveItem = new DriveItem
        {
            Id = driveItemId,
            OwnerId = userId.Value,
            ParentId = request.ParentId,
            Name = fileName,
            ItemType = DriveItemType.File,
            MimeType = request.File.ContentType,
            Size = request.File.Length,
            Checksum = null,
            IsDeleted = false,
            DeletedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        var fileVersion = new FileVersion
        {
            Id = fileVersionId,
            DriveItemId = driveItemId,
            VersionNumber = 1,
            S3Bucket = _fileStorage.BucketName,
            S3ObjectKey = objectKey,
            Size = request.File.Length,
            Checksum = null,
            MimeType = request.File.ContentType,
            IsCurrent = true,
            CreatedBy = userId.Value,
            CreatedAt = now
        };

        var uploaded = false;

        try
        {
            await _fileStorage.UploadAsync(
                objectKey,
                request.File.Content,
                request.File.ContentType,
                cancellationToken);

            uploaded = true;

            await _driveItemRepository.AddAsync(
                driveItem,
                cancellationToken);

            await _fileVersionRepository.AddAsync(
                fileVersion,
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
                                DriveItemId = driveItemId,
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

            return _mapper.Map<DriveItemResult>(driveItem);
        }
        catch
        {
            if (uploaded)
            {
                await _fileStorage.DeleteAsync(
                    objectKey,
                    CancellationToken.None);
            }

            throw;
        }
    }
}