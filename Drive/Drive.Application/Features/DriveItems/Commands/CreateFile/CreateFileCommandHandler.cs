using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFile;

public sealed class CreateFileCommandHandler
    : IRequestHandler<CreateFileCommand, DriveItemResult?>
{
    private readonly IRepository<DriveItem> _driveItemRepository;
    private readonly IRepository<FileVersion> _fileVersionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly IMapper _mapper;

    public CreateFileCommandHandler(
        IRepository<DriveItem> driveItemRepository,
        IRepository<FileVersion> fileVersionRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _fileVersionRepository = fileVersionRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _mapper = mapper;
    }

    public async Task<DriveItemResult?> Handle(
        CreateFileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return null;
        }

        var fileName = request.File.FileName.Trim();

        if (request.ParentId.HasValue)
        {
            var parent = await _driveItemRepository.SingleOrDefaultAsync(
                x =>
                    x.Id == request.ParentId.Value &&
                    x.OwnerId == userId.Value &&
                    !x.IsDeleted,
                cancellationToken);

            if (parent is null)
            {
                return null;
            }

            if (parent.ItemType != DriveItemType.Folder)
            {
                return null;
            }
        }

        var duplicateExists = await _driveItemRepository.AnyAsync(
            x =>
                x.OwnerId == userId.Value &&
                x.ParentId == request.ParentId &&
                x.Name.ToLower() == fileName.ToLower(),
            cancellationToken);

        if (duplicateExists)
        {
            return null;
        }

        var driveItemId = Guid.NewGuid();
        var fileVersionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var objectKey =
            $"files/{driveItemId}/{fileVersionId}";

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