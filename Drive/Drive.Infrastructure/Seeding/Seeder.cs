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
    private const string AdminPassword = "Password123!";

    private const string AdminEmail = "admin@test";
    private const string OwnerEmail = "owner@test";
    private const string DownloaderEmail = "downloader@test";
    private const string ViewerEmail = "viewer@test";
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
    private readonly IFileStorage _fileStorage;

    public Seeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        DriveDbContext dbContext,
        IPermissionMaterializer permissionMaterializer,
        IFileStorage fileStorage)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _permissionMaterializer = permissionMaterializer;
        _fileStorage = fileStorage;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        await _fileStorage.EnsureBucketExistsAsync(cancellationToken);

        // ============================================================
        // 1. ROLES & PERMISSIONS
        // ============================================================

        await SeedRoleAsync(
            "Admin",
            new[]
            {
                Permissions.DriveRead,
                Permissions.DriveDownload,
                Permissions.DriveDelete
            });

        await SeedRoleAsync(
            "Downloader",
            new[]
            {
                Permissions.DriveRead,
                Permissions.DriveDownload
            });

        await SeedRoleAsync(
            "Viewer",
            new[]
            {
                Permissions.DriveRead
            });

        // ============================================================
        // 2. TEST USERS
        // ============================================================

        var admin = await SeedUserAsync(
            AdminEmail,
            "Drive Admin User",
            AdminPassword);

        if (!await _userManager.IsInRoleAsync(admin, "Admin"))
        {
            await _userManager.AddToRoleAsync(admin, "Admin");
        }

        var owner = await SeedUserAsync(
            OwnerEmail,
            "Drive Test Owner");

        var downloader = await SeedUserAsync(
            DownloaderEmail,
            "Drive Test Downloader");

        var viewer = await SeedUserAsync(
            ViewerEmail,
            "Drive Test Viewer");

        var noAccess = await SeedUserAsync(
            NoAccessEmail,
            "Drive Test No Access");

        // ============================================================
        // 3. TEST DRIVE ITEMS
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
        // ============================================================

        var viewerRole = await _roleManager.FindByNameAsync("Viewer");
        if (viewerRole is null)
        {
            throw new InvalidOperationException("Seed role 'Viewer' was not found.");
        }

        var downloaderRole = await _roleManager.FindByNameAsync("Downloader");
        if (downloaderRole is null)
        {
            throw new InvalidOperationException("Seed role 'Downloader' was not found.");
        }

        await SeedRoleAssignmentAsync(
            SeedSharedFolderId,
            viewer.Id,
            viewerRole.Id,
            owner.Id);

        await SeedRoleAssignmentAsync(
            SeedSharedFolderId,
            downloader.Id,
            downloaderRole.Id,
            owner.Id);

        // ============================================================
        // 5. MATERIALIZE INHERITED PERMISSIONS
        // ============================================================

        await _permissionMaterializer.MaterializeAsync(
            SeedSharedFolderId,
            viewer.Id,
            viewerRole.Id,
            owner.Id,
            cancellationToken);

        await _permissionMaterializer.MaterializeAsync(
            SeedSharedFolderId,
            downloader.Id,
            downloaderRole.Id,
            owner.Id,
            cancellationToken);
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

        foreach (var obsoleteClaim in existingClaims.Where(x => x.Type == "permission" && !permissions.Contains(x.Value)))
        {
            await _roleManager.RemoveClaimAsync(role, obsoleteClaim);
        }

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
        string displayName,
        string password = SeedPassword)
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
            password);

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
        long? size = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DriveItems
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (existing is null)
        {
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

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (itemType == DriveItemType.File)
        {
            var existingVersion = await _dbContext.FileVersions
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(v => v.DriveItemId == id && v.IsCurrent, cancellationToken);

            if (existingVersion is null)
            {
                var versionId = Guid.NewGuid();
                var objectKey = $"files/{id}/{versionId}";
                var sampleText = $"Sample seed content for file: {name}\r\nItem ID: {id}\r\nCreated At: {createdAt:O}";
                var sampleBytes = System.Text.Encoding.UTF8.GetBytes(sampleText);

                if (!await _fileStorage.ExistsAsync(objectKey, cancellationToken))
                {
                    using var stream = new MemoryStream(sampleBytes);
                    await _fileStorage.UploadAsync(
                        objectKey,
                        stream,
                        mimeType ?? "text/plain",
                        cancellationToken);
                }

                _dbContext.FileVersions.Add(
                    new FileVersion
                    {
                        Id = versionId,
                        DriveItemId = id,
                        VersionNumber = 1,
                        S3Bucket = _fileStorage.BucketName,
                        S3ObjectKey = objectKey,
                        Size = sampleBytes.Length,
                        Checksum = null,
                        MimeType = mimeType ?? "text/plain",
                        IsCurrent = true,
                        CreatedBy = ownerId,
                        CreatedAt = createdAt
                    });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
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
            if (!existing.IsDirect)
            {
                throw new InvalidOperationException(
                    $"Expected direct assignment for user '{userId}' " +
                    $"on drive item '{driveItemId}'.");
            }

            if (existing.RoleId != roleId)
            {
                existing.RoleId = roleId;
                existing.SourceItemId = null;
                existing.IsDirect = true;
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
                IsDirect = true,
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await _dbContext.SaveChangesAsync();
    }
}