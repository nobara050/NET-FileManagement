using System.Security.Claims;
using Drive.Application.Common.Interfaces;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Drive.Infrastructure.Authorization;

public class PermissionService : IPermissionService
{
    private const string PermissionClaimType = "permission";

    private readonly DriveDbContext _dbContext;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        DriveDbContext dbContext,
        ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid driveItemId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var ownerId = await _dbContext.DriveItems
            .AsNoTracking()
            .Where(x => x.Id == driveItemId)
            .Select(x => (Guid?)x.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (ownerId is null)
        {
            return false;
        }

        if (ownerId.Value == userId)
        {
            return true;
        }

        var hasPermission = await (
            from assignment in _dbContext.DriveItemRoleAssignments.AsNoTracking()
            join roleClaim in _dbContext.RoleClaims.AsNoTracking()
                on assignment.RoleId equals roleClaim.RoleId
            where assignment.DriveItemId == driveItemId
                  && assignment.UserId == userId
                  && roleClaim.ClaimType == PermissionClaimType
                  && roleClaim.ClaimValue == permission
            select assignment.Id
        ).AnyAsync(cancellationToken);

        return hasPermission;
    }
}