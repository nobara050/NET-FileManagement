namespace Drive.Application.Common.Interfaces;

public interface IPermissionMaterializer
{
    Task MaterializeAsync(
        Guid driveItemId,
        Guid userId,
        Guid roleId,
        Guid createdBy,
        CancellationToken cancellationToken = default);

    Task RemoveInheritedAsync(
        Guid sourceItemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveInheritedFromScopeAsync(
        Guid driveItemId,
        Guid userId,
        Guid sourceItemId,
        CancellationToken cancellationToken = default);
}