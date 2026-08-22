namespace Drive.Application.Common.Interfaces;

public interface IPermissionMaterializer
{
    Task MaterializeAsync(
        Guid driveItemId,
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task RemoveInheritedAsync(
        Guid sourceItemId,
        Guid userId,
        CancellationToken cancellationToken = default);
}