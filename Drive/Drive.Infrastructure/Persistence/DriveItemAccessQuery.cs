using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence;

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
        CancellationToken cancellationToken = default)
    {
        var query = BuildAccessibleQuery(
            userId,
            parentId,
            searchTerm,
            itemType,
            permission);

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
        var normalizedSearchTerm =
            searchTerm?.Trim().ToLowerInvariant();

        return _dbContext.DriveItems
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.ParentId == parentId &&
                (itemType == null || x.ItemType == itemType.Value) &&
                (string.IsNullOrEmpty(normalizedSearchTerm) ||
                 x.Name.ToLower().Contains(normalizedSearchTerm)) &&
                (
                    x.OwnerId == userId ||
                    _dbContext.DriveItemRoleAssignments.Any(assignment =>
                        assignment.DriveItemId == x.Id &&
                        assignment.UserId == userId &&
                        _dbContext.RoleClaims.Any(roleClaim =>
                            roleClaim.RoleId == assignment.RoleId &&
                            roleClaim.ClaimType == "permission" &&
                            roleClaim.ClaimValue == permission)
                    )
                ));
    }
}