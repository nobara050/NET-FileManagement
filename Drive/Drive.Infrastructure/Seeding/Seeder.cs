using Drive.Application.Common.Authorization;
using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Drive.Infrastructure.Identity;
using Drive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Seeding;

public sealed class Seeder
{
    private const string SeedPassword = "Password123!";

    private const string OwnerEmail = "owner@test";
    private const string ViewerEmail = "viewer@test";
    private const string EditorEmail = "editor@test";
    private const string NoAccessEmail = "noaccess@test";

    private static readonly Guid SeedRootId =
    Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid SeedPrivateFolderId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");

    private static readonly Guid SeedPrivateFileId =
        Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly Guid SeedSharedFolderId =
        Guid.Parse("10000000-0000-0000-0000-000000000004");

    private static readonly Guid SeedSharedFileAId =
        Guid.Parse("10000000-0000-0000-0000-000000000005");

    private static readonly Guid SeedSharedFileBId =
        Guid.Parse("10000000-0000-0000-0000-000000000006");

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly DriveDbContext _dbContext;
    private readonly IPermissionMaterializer _permissionMaterializer;

    public Seeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        DriveDbContext dbContext,
        IPermissionMaterializer permissionMaterializer)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _permissionMaterializer = permissionMaterializer;
    }

    public async Task SeedAsync(
    CancellationToken cancellationToken = default)
    {
        // ============================================================
        // 1. ROLES & PERMISSIONS
        //
        // Role = permission profile.
        // Runtime authorization MUST check permission claims,
        // NOT hard-code role names such as Viewer / Editor.
        // ============================================================

        await SeedRoleAsync(
            "Viewer",
            new[]
            {
            Permissions.DriveRead,
            Permissions.DriveDownload
            });

        await SeedRoleAsync(
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

        // ============================================================
        // 2. TEST USERS
        //
        // Development/manual authorization test accounts only.
        // These users exist so the permission matrix can be tested
        // without manually registering accounts.
        //
        // Runtime authorization MUST NOT depend on these email addresses.
        // ============================================================

        var owner = await SeedUserAsync(
            OwnerEmail,
            "Drive Test Owner");

        var viewer = await SeedUserAsync(
            ViewerEmail,
            "Drive Test Viewer");

        var editor = await SeedUserAsync(
            EditorEmail,
            "Drive Test Editor");

        var noAccess = await SeedUserAsync(
            NoAccessEmail,
            "Drive Test No Access");

        // ============================================================
        // 3. TEST DRIVE ITEMS
        //
        // Dataset:
        //
        // Owner Root
        // ├── Private Folder
        // │   └── Private File
        // │
        // └── Shared Folder
        //     ├── Shared File A
        //     └── Shared File B
        //
        // All items belong to the Owner user.
        // Permissions are assigned in a later section.
        // ============================================================

        var now = DateTimeOffset.UtcNow;

        await SeedDriveItemAsync(
            SeedRootId,
            owner.Id,
            null,
            "Seed Owner Root",
            DriveItemType.Folder,
            now);

        await SeedDriveItemAsync(
            SeedPrivateFolderId,
            owner.Id,
            SeedRootId,
            "Seed Private Folder",
            DriveItemType.Folder,
            now);

        await SeedDriveItemAsync(
            SeedPrivateFileId,
            owner.Id,
            SeedPrivateFolderId,
            "Seed Private File.txt",
            DriveItemType.File,
            now,
            "text/plain",
            1024);

        await SeedDriveItemAsync(
            SeedSharedFolderId,
            owner.Id,
            SeedRootId,
            "Seed Shared Folder",
            DriveItemType.Folder,
            now);

        await SeedDriveItemAsync(
            SeedSharedFileAId,
            owner.Id,
            SeedSharedFolderId,
            "Seed Shared File A.txt",
            DriveItemType.File,
            now,
            "text/plain",
            2048);

        await SeedDriveItemAsync(
            SeedSharedFileBId,
            owner.Id,
            SeedSharedFolderId,
            "Seed Shared File B.pdf",
            DriveItemType.File,
            now,
            "application/pdf",
            4096);

        // ============================================================
        // 4. EXPLICIT ROLE ASSIGNMENTS
        //
        // Viewer and Editor are assigned explicitly to Shared Folder.
        //
        // IMPORTANT:
        // Runtime authorization does NOT depend on the role name.
        // The role's "permission" claims determine what the user can do.
        //
        // Viewer -> drive.read, drive.download
        // Editor -> drive.read, drive.download, drive.create, ...
        // ============================================================

        var viewerRole = await _roleManager.FindByNameAsync("Viewer");

        if (viewerRole is null)
        {
            throw new InvalidOperationException(
                "Seed role 'Viewer' was not found.");
        }

        var editorRole = await _roleManager.FindByNameAsync("Editor");

        if (editorRole is null)
        {
            throw new InvalidOperationException(
                "Seed role 'Editor' was not found.");
        }

        await SeedRoleAssignmentAsync(
            SeedSharedFolderId,
            viewer.Id,
            viewerRole.Id,
            owner.Id);

        await SeedRoleAssignmentAsync(
            SeedSharedFolderId,
            editor.Id,
            editorRole.Id,
            owner.Id);

        // ============================================================
        // 5. MATERIALIZE INHERITED PERMISSIONS
        //
        // Shared Folder permissions are inherited by:
        // - Seed Shared File A.txt
        // - Seed Shared File B.pdf
        //
        // We intentionally use the existing materializer instead of
        // manually inserting inherited assignments.
        // ============================================================

        await _permissionMaterializer.MaterializeAsync(
            SeedSharedFolderId,
            viewer.Id,
            viewerRole.Id);

        await _permissionMaterializer.MaterializeAsync(
            SeedSharedFolderId,
            editor.Id,
            editorRole.Id);
    }

    private async Task SeedRoleAsync(
    string roleName,
    IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);

        if (role is null)
        {
            role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };

            var createResult = await _roleManager.CreateAsync(role);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': " +
                    string.Join(
                        ", ",
                        createResult.Errors.Select(x => x.Description)));
            }
        }

        var existingClaims =
            await _roleManager.GetClaimsAsync(role);

        foreach (var permission in permissions)
        {
            var exists = existingClaims.Any(x =>
                x.Type == "permission" &&
                x.Value == permission);

            if (exists)
            {
                continue;
            }

            var claimResult = await _roleManager.AddClaimAsync(
                role,
                new Claim("permission", permission));

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

    private async Task<ApplicationUser> SeedUserAsync(
    string email,
    string displayName)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(
            user,
            SeedPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create seed user '{email}': " +
                string.Join(
                    ", ",
                    result.Errors.Select(x => x.Description)));
        }

        return user;
    }

    private async Task SeedDriveItemAsync(
    Guid id,
    Guid ownerId,
    Guid? parentId,
    string name,
    DriveItemType itemType,
    DateTimeOffset createdAt,
    string? mimeType = null,
    long? size = null)
    {
        var existing = await _dbContext.DriveItems
            .SingleOrDefaultAsync(x => x.Id == id);

        if (existing is not null)
        {
            return;
        }

        _dbContext.DriveItems.Add(
            new DriveItem
            {
                Id = id,
                OwnerId = ownerId,
                ParentId = parentId,
                Name = name,
                ItemType = itemType,
                MimeType = mimeType,
                Size = size,
                Checksum = null,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedRoleAssignmentAsync(
    Guid driveItemId,
    Guid userId,
    Guid roleId,
    Guid createdBy)
    {
        var existing = await _dbContext.DriveItemRoleAssignments
            .SingleOrDefaultAsync(x =>
                x.DriveItemId == driveItemId &&
                x.UserId == userId);

        if (existing is not null)
        {
            if (!existing.IsExplicit)
            {
                throw new InvalidOperationException(
                    $"Expected explicit assignment for user '{userId}' " +
                    $"on drive item '{driveItemId}'.");
            }

            if (existing.RoleId != roleId)
            {
                existing.RoleId = roleId;
                existing.SourceItemId = null;
                existing.IsExplicit = true;
                existing.CreatedBy = createdBy;
                existing.CreatedAt = DateTimeOffset.UtcNow;

                await _dbContext.SaveChangesAsync();
            }

            return;
        }

        _dbContext.DriveItemRoleAssignments.Add(
            new DriveItemRoleAssignment
            {
                Id = Guid.NewGuid(),
                DriveItemId = driveItemId,
                UserId = userId,
                RoleId = roleId,
                SourceItemId = null,
                IsExplicit = true,
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await _dbContext.SaveChangesAsync();
    }
}