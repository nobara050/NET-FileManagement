using Drive.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Drive.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        await SeedRoleAsync(
            roleManager,
            "Viewer",
            new[]
            {
                Permissions.DriveRead,
                Permissions.DriveDownload
            });

        await SeedRoleAsync(
            roleManager,
            "Editor",
            new[]
            {
                Permissions.DriveRead,
                Permissions.DriveDownload,
                Permissions.DriveCreate,
                Permissions.DriveUpdate,
                Permissions.DriveDelete,
                Permissions.DriveMove,
                Permissions.DriveCopy
            });
    }

    private static async Task SeedRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName,
        IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);

        if (role is null)
        {
            role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };

            var createResult = await roleManager.CreateAsync(role);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': " +
                    string.Join(
                        ", ",
                        createResult.Errors.Select(x => x.Description)));
            }
        }

        foreach (var permission in permissions)
        {
            var existingClaims =
                await roleManager.GetClaimsAsync(role);

            var exists = existingClaims.Any(x =>
                x.Type == "permission" &&
                x.Value == permission);

            if (exists)
            {
                continue;
            }

            var claimResult = await roleManager.AddClaimAsync(
                role,
                new System.Security.Claims.Claim(
                    "permission",
                    permission));

            if (!claimResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to add permission '{permission}' " +
                    $"to role '{roleName}': " +
                    string.Join(
                        ", ",
                        claimResult.Errors.Select(x => x.Description)));
            }
        }
    }
}