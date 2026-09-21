using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence.Queries;

public sealed class DriveItemAccessQuery : IDriveItemAccessQuery
{
    private readonly DriveDbContext _dbContext;

    public DriveItemAccessQuery(DriveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DriveItem>> ListAccessibleAsync(
        Guid userId,
        Guid? parentId,
        string? searchTerm,
        DriveItemType? itemType,
        string permission,
        int pageNumber = 0,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("DriveItemAccessQuery.ListAccessibleAsync", System.Diagnostics.ActivityKind.Internal);

        var query = BuildAccessibleQuery(
            userId,
            parentId,
            searchTerm,
            itemType,
            permission);

        query = query
            .OrderByDescending(x => x.ItemType == DriveItemType.Folder)
            .ThenBy(x => x.Name);

        if (pageNumber > 0 && pageSize > 0)
        {
            query = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAccessibleAsync(
        Guid userId,
        Guid? parentId,
        string? searchTerm,
        DriveItemType? itemType,
        string permission,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("DriveItemAccessQuery.CountAccessibleAsync", System.Diagnostics.ActivityKind.Internal);

        var query = BuildAccessibleQuery(
            userId,
            parentId,
            searchTerm,
            itemType,
            permission);

        return await query.CountAsync(cancellationToken);
    }

    private IQueryable<DriveItem> BuildAccessibleQuery(
        Guid userId,
        Guid? parentId,
        string? searchTerm,
        DriveItemType? itemType,
        string permission)
    {
        var query = _dbContext.DriveItems
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (parentId.HasValue)
        {
            query = query.Where(x => x.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(x => x.ParentId == null);
        }

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        // Accessible if the user is the owner OR has an active permission claim
        query = query.Where(item =>
            item.OwnerId == userId ||
            _dbContext.DriveItemRoleAssignments.Any(assignment =>
                assignment.DriveItemId == item.Id &&
                assignment.UserId == userId &&
                _dbContext.RoleClaims.Any(rc =>
                    rc.RoleId == assignment.RoleId &&
                    rc.ClaimType == "permission" &&
                    rc.ClaimValue == permission)));

        return query;
    }
}
