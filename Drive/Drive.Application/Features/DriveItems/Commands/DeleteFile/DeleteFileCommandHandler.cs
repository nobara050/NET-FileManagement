using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.DeleteFile;

public sealed class DeleteFileCommandHandler
    : IRequestHandler<DeleteFileCommand, bool>
{
    private readonly IRepository<DriveItem> _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFileCommandHandler(
        IRepository<DriveItem> driveItemRepository,
        ICurrentUserService currentUserService)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(
        DeleteFileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return false;
        }

        var file = await _driveItemRepository.SingleOrDefaultAsync(
            x =>
                x.Id == request.DriveItemId &&
                x.OwnerId == userId.Value &&
                x.ItemType == DriveItemType.File &&
                !x.IsDeleted,
            cancellationToken);

        if (file is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        file.IsDeleted = true;
        file.DeletedAt = now;
        file.UpdatedAt = now;

        _driveItemRepository.Update(file);

        await _driveItemRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}