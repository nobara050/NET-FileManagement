using Drive.Application.Common.Authorization;

namespace Drive.Application.Common.Interfaces;

public interface IDriveItemAuthorizationService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid driveItemId,
        string permission,
        CancellationToken cancellationToken = default);
}

