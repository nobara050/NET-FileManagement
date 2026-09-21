using Drive.Domain.Entities;

namespace Drive.Application.Common.Interfaces;

public interface IDriveItemRoleAssignmentRepository : IRepository<DriveItemRoleAssignment>
{
    Task<IReadOnlyList<DriveItemRoleAssignment>> ListByDriveItemIdAsync(
        Guid driveItemId,
        CancellationToken cancellationToken = default);

    Task<DriveItemRoleAssignment?> GetAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DriveItemRoleAssignment?> GetDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task RemoveInheritedByItemIdsAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken cancellationToken = default);
}
