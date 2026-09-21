using Drive.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Drive.Application.Features.Auth.Commands.UploadAvatar;

public sealed class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, string?>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserManagementService _userManagementService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UploadAvatarCommandHandler> _logger;

    public UploadAvatarCommandHandler(
        ICurrentUserService currentUserService,
        IUserManagementService userManagementService,
        IFileStorage fileStorage,
        ILogger<UploadAvatarCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _userManagementService = userManagementService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<string?> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return null;
        }

        var user = await _userManagementService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return null;
        }

        // If the user already has an existing avatar in S3, delete the old avatar object
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            var avatarIndex = user.AvatarUrl.IndexOf("avatars/", StringComparison.OrdinalIgnoreCase);
            if (avatarIndex >= 0)
            {
                var oldObjectKey = user.AvatarUrl.Substring(avatarIndex);
                try
                {
                    await _fileStorage.DeleteAsync(oldObjectKey, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old avatar object {OldKey} for user {UserId}", oldObjectKey, userId.Value);
                }
            }
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var newObjectKey = $"avatars/{userId.Value}/{Guid.NewGuid()}{extension}";

        await _fileStorage.UploadAsync(
            newObjectKey,
            request.File.Content,
            request.File.ContentType,
            cancellationToken);

        var avatarUrl = _fileStorage.GetFileUrl(newObjectKey);

        var updated = await _userManagementService.UpdateAvatarAsync(userId.Value, avatarUrl, cancellationToken);
        if (!updated)
        {
            return null;
        }

        return avatarUrl;
    }
}
