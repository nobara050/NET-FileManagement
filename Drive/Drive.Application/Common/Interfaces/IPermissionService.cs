namespace Drive.Application.Common.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid driveItemId,
        string permission,
        CancellationToken cancellationToken = default);
}