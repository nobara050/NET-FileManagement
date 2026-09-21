using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Roles.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Drive.Infrastructure.Identity;

public sealed class RoleService : IRoleService
{
    private const string PermissionClaimType = "permission";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleService(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<RoleResult>> ListRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var results = new List<RoleResult>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);

            results.Add(new RoleResult
            {
                RoleId = role.Id,
                Name = role.Name!,
                Claims = claims
                    .Where(c => c.Type == PermissionClaimType)
                    .Select(c => c.Value)
                    .ToList()
            });
        }

        return results;
    }

    public async Task<RoleResult?> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role is null)
        {
            return null;
        }

        var claims = await _roleManager.GetClaimsAsync(role);

        return new RoleResult
        {
            RoleId = role.Id,
            Name = role.Name!,
            Claims = claims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToList()
        };
    }

    public async Task<RoleResult?> CreateRoleAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var existing = await _roleManager.FindByNameAsync(name);

        if (existing is not null)
        {
            return null;
        }

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant()
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return null;
        }

        return new RoleResult
        {
            RoleId = role.Id,
            Name = role.Name!,
            Claims = []
        };
    }

    public async Task<bool> RenameRoleAsync(
        Guid roleId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role is null)
        {
            return false;
        }

        role.Name = newName;
        role.NormalizedName = newName.ToUpperInvariant();

        var result = await _roleManager.UpdateAsync(role);

        return result.Succeeded;
    }

    public async Task<bool> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role is null)
        {
            return false;
        }

        var result = await _roleManager.DeleteAsync(role);

        return result.Succeeded;
    }

    public async Task<bool> AddClaimAsync(
        Guid roleId,
        string claimValue,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role is null)
        {
            return false;
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);

        var alreadyExists = existingClaims.Any(c =>
            c.Type == PermissionClaimType &&
            c.Value == claimValue);

        if (alreadyExists)
        {
            // Idempotent — claim already present, treat as success.
            return true;
        }

        var result = await _roleManager.AddClaimAsync(
            role,
            new Claim(PermissionClaimType, claimValue));

        return result.Succeeded;
    }

    public async Task<bool> RemoveClaimAsync(
        Guid roleId,
        string claimValue,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role is null)
        {
            return false;
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);

        var claim = existingClaims.FirstOrDefault(c =>
            c.Type == PermissionClaimType &&
            c.Value == claimValue);

        if (claim is null)
        {
            return false;
        }

        var result = await _roleManager.RemoveClaimAsync(role, claim);

        return result.Succeeded;
    }
}
