using Drive.Application.Common.Authorization;
using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence.Queries;

public sealed class SharedDriveItemQuery : ISharedDriveItemQuery
{
    private readonly DriveDbContext _dbContext;

    public SharedDriveItemQuery(DriveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DriveItem>> ListSharedAsync(
        Guid userId,
        DriveItemType? itemType,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("SharedDriveItemQuery.ListSharedAsync", System.Diagnostics.ActivityKind.Internal);

        var normalizedSearchTerm = searchTerm?.Trim().ToLowerInvariant();

        var query = _dbContext.DriveItems
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                (itemType == null || x.ItemType == itemType.Value) &&
                (string.IsNullOrEmpty(normalizedSearchTerm) || x.Name.ToLower().Contains(normalizedSearchTerm)) &&
                _dbContext.DriveItemRoleAssignments.Any(a =>
                    a.DriveItemId == x.Id &&
                    a.UserId == userId &&
                    a.IsDirect &&
                    _dbContext.RoleClaims.Any(rc =>
                        rc.RoleId == a.RoleId &&
                        rc.ClaimType == "permission" &&
                        rc.ClaimValue == Permissions.DriveRead)));

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSharedAsync(
        Guid userId,
        DriveItemType? itemType,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        using var activity = Drive.Application.Common.Telemetry.ActivitySources.Infrastructure
            .StartActivity("SharedDriveItemQuery.CountSharedAsync", System.Diagnostics.ActivityKind.Internal);

        var normalizedSearchTerm = searchTerm?.Trim().ToLowerInvariant();

        return await _dbContext.DriveItems
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                (itemType == null || x.ItemType == itemType.Value) &&
                (string.IsNullOrEmpty(normalizedSearchTerm) || x.Name.ToLower().Contains(normalizedSearchTerm)) &&
                _dbContext.DriveItemRoleAssignments.Any(a =>
                    a.DriveItemId == x.Id &&
                    a.UserId == userId &&
                    a.IsDirect &&
                    _dbContext.RoleClaims.Any(rc =>
                        rc.RoleId == a.RoleId &&
                        rc.ClaimType == "permission" &&
                        rc.ClaimValue == Permissions.DriveRead)))
            .CountAsync(cancellationToken);
    }
}
