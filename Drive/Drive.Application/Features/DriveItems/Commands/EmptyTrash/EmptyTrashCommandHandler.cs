using Drive.Application.Common.Interfaces;
using Drive.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Drive.Application.Features.DriveItems.Commands.EmptyTrash;

public sealed class EmptyTrashCommandHandler : IRequestHandler<EmptyTrashCommand, int>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<EmptyTrashCommandHandler> _logger;

    public EmptyTrashCommandHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<EmptyTrashCommandHandler> logger)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<int> Handle(
        EmptyTrashCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return 0;
        }

        var deletedItems = await _driveItemRepository.GetUserDeletedItemsAsync(
            userId.Value,
            cancellationToken);

        if (deletedItems.Count == 0)
        {
            return 0;
        }

        // Delete S3 binaries for all files in the trash
        foreach (var fileItem in deletedItems.Where(x => x.ItemType == DriveItemType.File))
        {
            foreach (var version in fileItem.Versions)
            {
                await _fileStorage.DeleteAsync(version.S3ObjectKey, cancellationToken);
            }
        }

        await _driveItemRepository.HardDeleteItemsAsync(deletedItems, cancellationToken);

        return deletedItems.Count;
    }
}
