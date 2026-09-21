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
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("PermissionMaterializer.MaterializeAsync", System.Diagnostics.ActivityKind.Internal);

        var descendants = await GetDescendantsAsync(
            driveItemId,
            cancellationToken);

        if (descendants.Count == 0)
        {
            return;
        }

        var descendantIds = descendants.Select(x => x.Id).ToList();

        var existingAssignments = await _dbContext.DriveItemRoleAssignments
            .Where(x => descendantIds.Contains(x.DriveItemId) && x.UserId == userId)
            .ToListAsync(cancellationToken);

        var existingMap = existingAssignments.ToDictionary(x => x.DriveItemId);

        var now = DateTimeOffset.UtcNow;
        var toAdd = new List<DriveItemRoleAssignment>();

        foreach (var descendant in descendants)
        {
            if (existingMap.TryGetValue(descendant.Id, out var existing))
            {
                if (existing.IsDirect)
                {
                    continue;
                }

                existing.RoleId = roleId;
                existing.SourceItemId = driveItemId;
            }
            else
            {
                toAdd.Add(new DriveItemRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = descendant.Id,
                    UserId = userId,
                    RoleId = roleId,
                    SourceItemId = driveItemId,
                    IsDirect = false,
                    CreatedBy = createdBy,
                    CreatedAt = now
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await _dbContext.DriveItemRoleAssignments.AddRangeAsync(toAdd, cancellationToken);
        }
    }

    public async Task RemoveInheritedAsync(
        Guid sourceItemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("PermissionMaterializer.RemoveInheritedAsync", System.Diagnostics.ActivityKind.Internal);

        var assignments = await _dbContext.DriveItemRoleAssignments
            .Where(x =>
                x.UserId == userId &&
                !x.IsDirect &&
                x.SourceItemId == sourceItemId)
            .ToListAsync(cancellationToken);

        _dbContext.DriveItemRoleAssignments.RemoveRange(assignments);
    }

    public async Task RemoveInheritedFromScopeAsync(
        Guid driveItemId,
        Guid userId,
        Guid sourceItemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("PermissionMaterializer.RemoveInheritedFromScopeAsync", System.Diagnostics.ActivityKind.Internal);

        var descendants = await GetDescendantsAsync(
            driveItemId,
            cancellationToken);

        if (descendants.Count == 0)
        {
            return;
        }

        var descendantIds = descendants.Select(x => x.Id).ToList();

        var assignments = await _dbContext.DriveItemRoleAssignments
            .Where(x =>
                x.UserId == userId &&
                !x.IsDirect &&
                x.SourceItemId == sourceItemId &&
                descendantIds.Contains(x.DriveItemId))
            .ToListAsync(cancellationToken);

        if (assignments.Count > 0)
        {
            _dbContext.DriveItemRoleAssignments.RemoveRange(assignments);
        }
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
                .Where(x => x.ParentId == currentId && !x.IsDeleted)
                .Select(x => new DriveItem
                {
                    Id = x.Id,
                    ParentId = x.ParentId,
                    ItemType = x.ItemType
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
