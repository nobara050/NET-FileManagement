using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Authorization;

public class PermissionMaterializer : IPermissionMaterializer
{
    private readonly DriveDbContext _dbContext;

    public PermissionMaterializer(DriveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task MaterializeAsync(
        Guid driveItemId,
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var descendants = await GetDescendantsAsync(
            driveItemId,
            cancellationToken);

        foreach (var descendant in descendants)
        {
            var existing = await _dbContext.DriveItemRoleAssignments
                .SingleOrDefaultAsync(
                    x =>
                        x.DriveItemId == descendant.Id &&
                        x.UserId == userId,
                    cancellationToken);

            if (existing is not null)
            {
                if (existing.IsExplicit)
                {
                    continue;
                }

                existing.RoleId = roleId;
                existing.SourceItemId = driveItemId;
                continue;
            }

            _dbContext.DriveItemRoleAssignments.Add(
                new DriveItemRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = descendant.Id,
                    UserId = userId,
                    RoleId = roleId,
                    SourceItemId = driveItemId,
                    IsExplicit = false,
                    CreatedBy = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveInheritedAsync(
        Guid sourceItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _dbContext.DriveItemRoleAssignments
            .Where(x =>
                x.UserId == userId &&
                !x.IsExplicit &&
                x.SourceItemId == sourceItemId)
            .ToListAsync(cancellationToken);

        _dbContext.DriveItemRoleAssignments.RemoveRange(assignments);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<DriveItem>> GetDescendantsAsync(
        Guid parentId,
        CancellationToken cancellationToken)
    {
        var result = new List<DriveItem>();

        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var children = await _dbContext.DriveItems
                .AsNoTracking()
                .Where(x => x.ParentId == currentId)
                .Select(x => new DriveItem
                {
                    Id = x.Id,
                    ParentId = x.ParentId
                })
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                result.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }
}