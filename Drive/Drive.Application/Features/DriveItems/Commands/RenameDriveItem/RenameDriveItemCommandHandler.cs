using AutoMapper;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RenameDriveItem;

public sealed class RenameDriveItemCommandHandler : IRequestHandler<RenameDriveItemCommand, DriveItemResult>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public RenameDriveItemCommandHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<DriveItemResult> Handle(
        RenameDriveItemCommand request,
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

        // Only the owner can rename an item
        if (item.OwnerId != userId.Value)
        {
            throw new ForbiddenException("Only the owner can rename an item.");
        }

        var newName = request.Name.Trim();
        if (string.Equals(item.Name, newName, StringComparison.Ordinal))
        {
            return _mapper.Map<DriveItemResult>(item);
        }

        var duplicateExists = await _driveItemRepository.IsDuplicateNameAsync(
            item.OwnerId,
            item.ParentId,
            newName,
            item.Id,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"An item with name '{newName}' already exists in this folder.");
        }

        item.Name = newName;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        _driveItemRepository.Update(item);
        await _driveItemRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DriveItemResult>(item);
    }
}
