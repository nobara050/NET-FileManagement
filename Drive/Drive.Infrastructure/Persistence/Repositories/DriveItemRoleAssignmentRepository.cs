using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence.Repositories;

public class DriveItemRoleAssignmentRepository
    : Repository<DriveItemRoleAssignment>, IDriveItemRoleAssignmentRepository
{
    public DriveItemRoleAssignmentRepository(DriveDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<DriveItemRoleAssignment>> ListByDriveItemIdAsync(
        Guid driveItemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(ListByDriveItemIdAsync));
        return await DbContext.DriveItemRoleAssignments
            .Where(x => x.DriveItemId == driveItemId)
            .ToListAsync(cancellationToken);
    }

    public async Task<DriveItemRoleAssignment?> GetAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetAssignmentAsync));
        return await DbContext.DriveItemRoleAssignments
            .SingleOrDefaultAsync(
                x => x.DriveItemId == driveItemId &&
                     x.UserId == userId,
                cancellationToken);
    }

    public async Task<DriveItemRoleAssignment?> GetDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetDirectAssignmentAsync));
        return await DbContext.DriveItemRoleAssignments
            .SingleOrDefaultAsync(
                x => x.DriveItemId == driveItemId &&
                     x.UserId == userId &&
                     x.IsDirect,
                cancellationToken);
    }

    public async Task<bool> ExistsDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(ExistsDirectAssignmentAsync));
        return await DbContext.DriveItemRoleAssignments
            .AnyAsync(
                x => x.DriveItemId == driveItemId &&
                     x.UserId == userId &&
                     x.IsDirect,
                cancellationToken);
    }

    public async Task RemoveDirectAssignmentAsync(
        Guid driveItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(RemoveDirectAssignmentAsync));
        await DbContext.DriveItemRoleAssignments
            .Where(x => x.DriveItemId == driveItemId &&
                        x.UserId == userId &&
                        x.IsDirect)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(RemoveByRoleIdAsync));
        await DbContext.DriveItemRoleAssignments
            .Where(x => x.RoleId == roleId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveInheritedByItemIdsAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(RemoveInheritedByItemIdsAsync));
        var idList = itemIds.ToList();
        if (idList.Count == 0) return;

        var inherited = await DbContext.DriveItemRoleAssignments
            .Where(x => idList.Contains(x.DriveItemId) && !x.IsDirect)
            .ToListAsync(cancellationToken);

        DbContext.DriveItemRoleAssignments.RemoveRange(inherited);
    }
}
