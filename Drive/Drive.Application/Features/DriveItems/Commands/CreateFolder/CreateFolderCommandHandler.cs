using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFolder;

public sealed class CreateFolderCommandHandler
    : IRequestHandler<CreateFolderCommand, DriveItemResult?>
{
    private readonly IRepository<DriveItem> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateFolderCommandHandler(
        IRepository<DriveItem> repository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<DriveItemResult?> Handle(
        CreateFolderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return null;
        }

        var folderName = request.Name.Trim();

        if (request.ParentId.HasValue)
        {
            var parent = await _repository.SingleOrDefaultAsync(
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

        var duplicateExists = await _repository.AnyAsync(
            x =>
                x.OwnerId == userId.Value &&
                x.ParentId == request.ParentId &&
                x.Name.ToLower() == folderName.ToLower(),
            cancellationToken);

        if (duplicateExists)
        {
            return null;
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

        await _repository.AddAsync(
            folder,
            cancellationToken);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<DriveItemResult>(folder);
    }
}