using Drive.Application.Common.Authorization;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.GetPreviewUrl;

public sealed class GetPreviewUrlQueryHandler
    : IRequestHandler<GetPreviewUrlQuery, DownloadUrlResult>
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IFileStorage _fileStorage;

    public GetPreviewUrlQueryHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IFileStorage fileStorage)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _fileStorage = fileStorage;
    }

    public async Task<DownloadUrlResult> Handle(
        GetPreviewUrlQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var driveItem = await _driveItemRepository.GetWithVersionsAsync(
            request.DriveItemId,
            cancellationToken);

        if (driveItem is null || driveItem.IsDeleted)
        {
            throw new NotFoundException("File not found.");
        }

        if (driveItem.ItemType != DriveItemType.File)
        {
            throw new BadRequestException("The target item is not a file.");
        }

        // Owners have implicit full access; non-owners must hold drive.read or drive.download.
        if (driveItem.OwnerId != userId.Value)
        {
            var hasRead = await _permissionService.HasPermissionAsync(
                userId.Value,
                driveItem.Id,
                Permissions.DriveRead,
                cancellationToken);

            var hasDownload = hasRead || await _permissionService.HasPermissionAsync(
                userId.Value,
                driveItem.Id,
                Permissions.DriveDownload,
                cancellationToken);

            if (!hasRead && !hasDownload)
            {
                throw new ForbiddenException(
                    "You do not have permission to preview this file.");
            }
        }

        var currentVersion = driveItem.Versions
            .SingleOrDefault(v => v.IsCurrent);

        if (currentVersion is null)
        {
            throw new NotFoundException("No current version found for this file.");
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(DefaultExpiry);

        var url = await _fileStorage.GeneratePresignedPreviewUrlAsync(
            currentVersion.S3ObjectKey,
            currentVersion.MimeType,
            DefaultExpiry,
            cancellationToken);

        return new DownloadUrlResult
        {
            Url = url,
            ExpiresAt = expiresAt
        };
    }
}
