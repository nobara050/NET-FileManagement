# Drive Backend — Architecture Reference

> Complete technical reference for the Drive file-management backend.  
> **Purpose**: An AI agent reading this file should fully understand every endpoint, entity, permission rule, and business logic without access to the source code.

---

## 1. Technology Stack

| Component | Detail |
|---|---|
| Framework | ASP.NET Core (.NET) — Web API |
| Architecture | Clean Architecture: Domain → Application → Infrastructure → Api |
| Database | PostgreSQL via EF Core + Npgsql |
| Identity | ASP.NET Core Identity (`IdentityUser<Guid>`, `IdentityRole<Guid>`) |
| Auth | JWT Bearer (access token) + SHA-256-hashed Refresh Token (7-day TTL) |
| File Storage | AWS S3-compatible — LocalStack in dev, real S3 in prod |
| CQRS | MediatR (Commands + Queries) |
| Validation | FluentValidation (runs as MediatR pipeline behavior) |
| Mapping | AutoMapper |
| Base URL (dev) | `https://localhost:{port}` |
| Swagger UI | `/swagger` — Development only |

---

## 2. Clean Architecture Layers

```
Drive.Domain          ← Entities, Enums. No external dependencies.
Drive.Application     ← Use cases (CQRS Handlers), Interfaces, DTOs.
Drive.Infrastructure  ← EF Core, Identity, JWT, S3, Repository implementations.
Drive.Api             ← Controllers, Request/Response models, Authorization handlers.
```

---

## 3. Domain Entities

### `DriveItem` — central entity representing a file or folder

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `OwnerId` | `Guid` | FK → ApplicationUser. Owner has implicit full access. |
| `ParentId` | `Guid?` | Null for root items |
| `Name` | `string` | File/folder name |
| `ItemType` | `DriveItemType` | `File = 1`, `Folder = 2` |
| `MimeType` | `string?` | e.g. `text/plain`, `application/pdf`. Null for folders. |
| `Size` | `long?` | Bytes. Null for folders. |
| `Checksum` | `string?` | Content hash. Currently null (populated in future). |
| `IsDeleted` | `bool` | Soft-delete flag |
| `DeletedAt` | `DateTimeOffset?` | Set when soft-deleted |
| `CreatedAt` | `DateTimeOffset` | |
| `UpdatedAt` | `DateTimeOffset` | |

Navigation: `Parent`, `Children`, `Versions`.

---

### `DriveItemRoleAssignment` — item-level RBAC table

Each row grants one user a role on one drive item.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `DriveItemId` | `Guid` | The item being shared |
| `UserId` | `Guid` | The user receiving the role |
| `RoleId` | `Guid` | FK → IdentityRole |
| `SourceItemId` | `Guid?` | Null for explicit grants; ancestor folder ID for inherited grants |
| `IsExplicit` | `bool` | `true` = manually assigned; `false` = propagated from parent folder |
| `CreatedBy` | `Guid` | The owner who triggered the assignment |
| `CreatedAt` | `DateTimeOffset` | |

---

### `FileVersion` — S3 version history per file

One row per upload. The active version has `IsCurrent = true`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `DriveItemId` | `Guid` | FK → DriveItem |
| `VersionNumber` | `int` | Monotonically increasing |
| `S3Bucket` | `string` | S3 bucket name |
| `S3ObjectKey` | `string` | `files/{driveItemId}/{fileVersionId}` |
| `Size` | `long` | |
| `Checksum` | `string?` | |
| `MimeType` | `string` | |
| `IsCurrent` | `bool` | Only one true per DriveItemId |
| `CreatedBy` | `Guid` | Uploader |
| `CreatedAt` | `DateTimeOffset` | |

---

### `RefreshToken` — stored as SHA-256 hash, never plaintext

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → ApplicationUser |
| `TokenHash` | `string` | SHA-256(rawToken) |
| `ExpiresAt` | `DateTimeOffset` | 7 days from creation |
| `CreatedAt` | `DateTimeOffset` | |
| `RevokedAt` | `DateTimeOffset?` | Null if still valid |
| `ReplacedByTokenId` | `Guid?` | Points to successor token on rotate |

---

### `ApplicationUser` — extends `IdentityUser<Guid>`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Email` | `string` | Unique. Also used as UserName. |
| `DisplayName` | `string` | Human-readable name |
| `AvatarUrl` | `string?` | Optional profile picture |

---

## 4. RBAC & Permission System

### 4.1 Permission strings (`ClaimType = "permission"`)

| Constant | String value | Meaning |
|---|---|---|
| `Permissions.DriveRead` | `drive.read` | List/view items |
| `Permissions.DriveDownload` | `drive.download` | Download files |
| `Permissions.DriveCreate` | `drive.create` | Upload files / create folders |
| `Permissions.DriveUpdate` | `drive.update` | Edit items |
| `Permissions.DriveDelete` | `drive.delete` | Delete items |
| `Permissions.DriveMove` | `drive.move` | Move items |
| `Permissions.DriveCopy` | `drive.copy` | Copy items |

### 4.2 Built-in roles (seeded at startup)

| Role name | Permissions |
|---|---|
| `Admin` | No drive permissions — has the ASP.NET Identity role claim `Admin` in JWT, granting access to admin-only endpoints. |
| `Viewer` | `drive.read`, `drive.download` |
| `Editor` | All 7 `drive.*` permissions |

### 4.3 Permission check flow (`PermissionService.HasPermissionAsync`)

1. Look up `DriveItem.OwnerId`. If `userId == OwnerId` → **grant immediately** (full access).
2. Otherwise: `JOIN DriveItemRoleAssignments ON (DriveItemId, UserId)` with `IdentityRoleClaims` filtering `ClaimType = "permission"` and `ClaimValue = <required permission>`. If any row matches → **grant**.
3. No match → **deny**.

### 4.4 Permission inheritance (`PermissionMaterializer`)

**On assign (MaterializeAsync):**
- BFS-walks all non-deleted descendants of the target folder.
- For each descendant: if the user already has an **explicit** assignment there → skip. If an inherited assignment exists → update its `RoleId` and `SourceItemId`. Otherwise → insert a new row with `IsExplicit = false`, `SourceItemId = <source folder>`.
- All inherited rows record the real assigning owner in `CreatedBy`.

**On remove/update (RemoveInheritedAsync):**
- Deletes all rows where `UserId = <user>` AND `IsExplicit = false` AND `SourceItemId = <source folder>`.
- UpdateAssignment then calls MaterializeAsync again with the new role to re-populate.

### 4.5 Assignment rules enforced in handlers

- Only the **item owner** can assign, update, or remove assignments.
- An owner **cannot assign a role to themselves** (they already have implicit full access).
- Duplicate check on `AssignRole` considers **only explicit** assignments — inherited rows do not block a new explicit assignment on the same item.
- `RemoveAssignment` removes both the inherited descendants first, then the explicit row (both saved to DB).

### 4.6 Folder Owner Authority & Shared Folder Constraints

- **Folder Owner Authority**: The owner of a parent folder has complete authority over all items contained within it. Even if an item inside was uploaded or owned by another user, the folder owner can delete it (soft-delete) or permanently delete it from trash.
- **Shared Folders Are Read-Only**: Non-owners cannot create new files or folders inside a shared folder (enforced in `CreateFileCommandHandler` and `CreateFolderCommandHandler` via `parent.OwnerId == userId.Value`).
- Non-owners cannot move or rename items inside shared folders. Shared items can only be viewed (and copied to the user's own drive if permitted).

### 4.7 Move Operation & Role Assignment Synchronization

- **Owner-Only**: Only the owner of an item can move it (`PATCH /api/drive-items/{id}/move`).
- **Destination Verification**: The destination folder must be owned by the caller (or `null` for root). Moving into another user's folder is forbidden.
- **Cycle Prevention**: A folder cannot be moved into itself or any of its own descendants.
- **Role Assignment Synchronization**:
  - **Explicit roles are preserved**: Any direct explicit share (`IsExplicit = true`) on the moved item or its descendants remains intact.
  - **Inherited roles are replaced**: All old inherited roles (`IsExplicit = false`) on the moved item and its descendants are purged. If moved to a destination folder, the destination folder's active role assignments are materialized onto the moved item and its descendants (skipping any user who already has an explicit role or is the owner).

### 4.8 Copy Operation Semantics

- **Files Only**: Only files can be copied (`POST /api/drive-items/{id}/copy`); folders cannot be copied (`400 Bad Request`).
- **Naming Rule**: Copied files append `_copy` before the extension (e.g. `report_copy.pdf`). If that name already exists in the destination folder, an incrementing index is added (e.g. `report_copy(1).pdf`).
- **Shared Files**: A file shared with the caller can be copied into the caller's own drive provided the caller has `drive.read` or `drive.copy` permission. The destination folder must be owned by the caller (or root).
- **S3 Server-Side Copy**: The underlying binary is duplicated directly inside S3 using `CopyObjectAsync` to avoid transferring bytes through the API server.

---

## 5. API Authorization Summary

| Endpoint group | Who can call |
|---|---|
| `POST /api/auth/register`, `/login`, `/refresh` | Public (no token needed) |
| `POST /api/auth/logout` | Any authenticated user (`[Authorize]`) |
| `GET /api/auth/me`, `GET /api/users/me` | Any authenticated user (`[Authorize]`) |
| `POST /api/auth/me/avatar` | Any authenticated user (`[Authorize]`) |
| `GET /api/users/search` | Any authenticated user (`[Authorize]`) |
| `GET /api/users`, `GET /api/users/{id}`, `DELETE /api/users/{id}` | Admin only (`[Authorize(Policy = "Admin")]`) |
| `GET /api/roles` | Any authenticated user |
| All other `POST/PUT/DELETE /api/roles/*` | Admin only (`[Authorize(Policy = "Admin")]`) |
| `GET/POST /api/drive-items` | Any authenticated user (permission/owner checks inside handlers) |
| `PATCH /api/drive-items/{id}/rename` | Owner only |
| `PATCH /api/drive-items/{id}/move` | Owner only |
| `POST /api/drive-items/{id}/copy` | Caller with `drive.read` or `drive.copy` (destination owned by caller) |
| `DELETE /api/drive-items/{id}` | Item owner or parent folder owner or user with `drive.delete` |
| `DELETE /api/drive-items/trash/{id}` | Item owner or parent folder owner |
| `DELETE /api/drive-items/trash` | Current user (empties all soft-deleted items owned by caller) |
| `GET/POST/PUT/DELETE /api/drive-items/{id}/assignments` | Item owner only |

**`Admin` policy**: `RequireAuthenticatedUser().RequireRole("Admin")` — role encoded in JWT.

**`PermissionRequirement` (attribute-based):**  
`PermissionAuthorizationHandler` reads `driveItemId`, `itemId`, or `id` from the route or `parentId` from the query/form, then calls `PermissionService.HasPermissionAsync`. In accordance with fail-closed security principles, if the item ID cannot be resolved from the request, the authorization check **fails** (`context.Fail()`) rather than succeeding by default.

---

## 6. API Endpoints — Full Reference

> All endpoints except auth register, login, and refresh require `Authorization: Bearer <access_token>`.

### 6.1 Auth — `/api/auth`

| Method | Path | Body | Success | Failure | Notes |
|---|---|---|---|---|---|
| `POST` | `/api/auth/register` | `{ email, password, displayName }` | `200 { userId }` | `400` validation | Public |
| `POST` | `/api/auth/login` | `{ email, password }` | `200 { accessToken, refreshToken }` | `401` | Public |
| `POST` | `/api/auth/refresh` | `{ refreshToken }` | `200 { accessToken, refreshToken }` | `401` invalid/expired | Public — rotates refresh token |
| `POST` | `/api/auth/logout` | — | `204` | `401` | `[Authorize]` Bearer token in header; revokes active refresh tokens for the user |
| `GET` | `/api/auth/me` | — | `200 CurrentUserResult` | `401` | `[Authorize]` — returns current profile `{ id, email, displayName, avatarUrl }` (aliased at `GET /api/users/me`) |
| `POST` | `/api/auth/me/avatar` | Form: `file` (image) | `200 { avatarUrl }` | `400` validation | `[Authorize]` — uploads to S3 `avatars/{userId}/{Guid}.{ext}`, deletes old avatar from S3 |

---

### 6.2 Users — `/api/users`

| Method | Path | Auth | Params | Success | Failure | Notes |
|---|---|---|---|---|---|---|
| `GET` | `/api/users` | Admin | — | `200 UserResult[]` | — | Admin only (`[Authorize(Policy = "Admin")]`) |
| `GET` | `/api/users/{userId}` | Admin | Path: `userId` | `200 UserResult` | `404` | Admin only |
| `DELETE` | `/api/users/{userId}` | Admin | Path: `userId` | `204` | `404` | Admin only |
| `GET` | `/api/users/me` | Authenticated | — | `200 CurrentUserResult` | `401` | Alias for `/api/auth/me` |
| `GET` | `/api/users/search` | Authenticated | Query: `query` (min 2 chars) | `200 UserResult[]` | `400` | Searches by email or display name prefix/substring (max 20) |

**`UserResult`**: `{ userId, email, displayName, avatarUrl? }`  
**`CurrentUserResult`**: `{ id, email, displayName, avatarUrl? }`

---

### 6.3 Roles — `/api/roles`

| Method | Path | Auth | Body/Params | Success | Failure |
|---|---|---|---|---|---|
| `GET` | `/api/roles` | Any authenticated | — | `200 RoleResult[]` | — |
| `GET` | `/api/roles/{roleId}` | Admin | Path: `roleId` | `200 RoleResult` | `404` |
| `POST` | `/api/roles` | Admin | `{ name }` | `201 RoleResult` | `409` duplicate name |
| `PUT` | `/api/roles/{roleId}/name` | Admin | `{ newName }` | `204` | `404` |
| `DELETE` | `/api/roles/{roleId}` | Admin | Path: `roleId` | `204` | `404` |
| `POST` | `/api/roles/{roleId}/claims` | Admin | `{ claimValue }` | `204` | `404`; `400` invalid claim |
| `DELETE` | `/api/roles/{roleId}/claims/{claimValue}` | Admin | Path params | `204` | `404` |

**`RoleResult`**: `{ roleId, name, claims: string[] }`  
**Valid `claimValue`**: one of the 7 `drive.*` permission strings (validated by FluentValidation).  
**DELETE role**: cleans up all `DriveItemRoleAssignment` rows referencing that `roleId` before deleting from Identity.

---

### 6.4 Drive Items — `/api/drive-items`

| Method | Path | Auth | Body/Query | Success | Failure |
|---|---|---|---|---|---|
| `GET` | `/api/drive-items` | Authenticated | Query: `parentId?`, `searchTerm?`, `itemType?`, `scope`, `pageNumber`, `pageSize` | `200 PagedResult<DriveItemResult>` | `200 empty` if no access |
| `POST` | `/api/drive-items/files` | Authenticated (Parent owner only) | Form: `parentId?`, `file` (IFormFile) | `201 DriveItemResult` | `null → 400` |
| `POST` | `/api/drive-items/folders` | Authenticated (Parent owner only) | JSON: `{ parentId?, name }` | `201 DriveItemResult` | `null → 400` |
| `PATCH` | `/api/drive-items/{id}/rename` | Authenticated (Owner only) | JSON: `{ newName }` | `200 DriveItemResponse` | `400`, `403` (not owner), `409` (duplicate name) |
| `PATCH` | `/api/drive-items/{id}/move` | Authenticated (Owner only) | JSON: `{ destinationFolderId? }` | `200 DriveItemResponse` | `400`, `403`, `409` (duplicate/cycle) |
| `POST` | `/api/drive-items/{id}/copy` | Authenticated (`drive.read` or `drive.copy`) | JSON: `{ destinationFolderId? }` | `201 DriveItemResponse` | `400` (folder copy not allowed), `403`, `404` |
| `DELETE` | `/api/drive-items/{driveItemId}` | Authenticated | Path: `driveItemId` | `204` | `403`, `404` (soft delete; owner or parent owner) |
| `POST` | `/api/drive-items/{driveItemId}/recover` | Authenticated (Owner only) | Path: `driveItemId` | `200 DriveItemResponse` | `404`, `403` (not owner), `409` (duplicate name or parent deleted) |
| `GET` | `/api/drive-items/deleted` | Authenticated | Query: `searchTerm?`, `itemType?`, `pageNumber`, `pageSize` | `200 PagedListDriveItemsResponse` | — |
| `DELETE` | `/api/drive-items/trash/{id}` | Authenticated (Owner or parent owner) | Path: `id` | `204` | `403`, `404` (hard deletes item + soft-deleted descendants + S3 binaries) |
| `DELETE` | `/api/drive-items/trash` | Authenticated | — | `204` | — (empties all soft-deleted items owned by caller) |

**`scope` values** (`ListDriveItemsScope`): `Owned` (default), `Shared`, `All`.  
- `Owned` + no `parentId`: items owned by caller at root.  
- `Shared` + no `parentId`: items **explicitly** shared with caller (IsExplicit=true).  
- Any `parentId`: lists children of that folder — caller must own it or have `drive.read`.

**CreateFile/Folder**: non-owners cannot create items in shared folders. If `parentId` is provided, the caller must be the owner of that parent folder (`parent.OwnerId == userId.Value`). Duplicate names within the same parent folder are rejected.

**DeleteDriveItem**: soft-delete (`IsDeleted=true`, `DeletedAt`). The caller must be the item's owner, the parent folder's owner, or have `drive.delete`. If a folder is deleted, all active descendants are soft-deleted with the same timestamp.

**HardDeleteDriveItem (Trash item delete)**: permanently removes a soft-deleted item from trash. Caller must be the item's owner or parent folder's owner. Traverses and purges all soft-deleted descendants leaf-first and deletes all S3 version binaries.

**EmptyTrash**: permanently removes all soft-deleted items owned by the caller and deletes their S3 binaries.

**RecoverDriveItem**: only the owner of the item can recover it. Checks for active duplicate names in destination path first (unaffected by other deleted items). If parent folder is deleted, recovery is blocked until parent folder is recovered. Recovering a folder restores all items cascaded with it.

**Automatic Hard Delete**: background service (`AutomaticHardDeleteBackgroundService`) permanently deletes items soft-deleted beyond `TrashSettings:RetentionDays` (default 30 days), deleting files from S3 and purging records leaf-first.

**`DriveItemResult`**: `{ id, ownerId, parentId?, name, itemType, mimeType?, size?, isDeleted, createdAt, updatedAt }`

**`PagedResult<T>`**: `{ items: T[], pageNumber, pageSize, totalCount, totalPages }` — pagination is pushed directly to the database query via EF Core `Skip` and `Take`.

---

### 6.5 Item Assignments — `/api/drive-items/{itemId}/assignments` — Item Owner Only

Authorization enforced inside the handler: caller must be the item's owner.

| Method | Path | Body/Params | Success | Failure |
|---|---|---|---|---|
| `GET` | `/api/drive-items/{itemId}/assignments` | — | `200 AssignmentResult[]` (explicit + inherited) | `404` |
| `POST` | `/api/drive-items/{itemId}/assignments` | `{ targetUserId?, targetEmail?, roleId }` | `204` | `400` (not owner, dupe explicit, self-assign, role not found) |
| `PUT` | `/api/drive-items/{itemId}/assignments/{targetUserId}` | `{ newRoleId }` | `204` | `404` |
| `DELETE` | `/api/drive-items/{itemId}/assignments/{targetUserId}` | — | `204` | `404` |

**`AssignmentResult`**: `{ userId, roleId, roleName, isExplicit, sourceItemId? }`  
- `sourceItemId` is populated for inherited rows — it identifies the ancestor folder that originated the permission.

**POST business rules**:
- Caller must be item owner.
- Target user can be identified by either `targetUserId` or `targetEmail`.
- Target user must not equal caller (no self-assignment).
- If an explicit assignment already exists for `(itemId, targetUserId)`, returns conflict/bad request.
- If an **inherited** assignment already exists for `(itemId, targetUserId)`, it is **promoted/updated** to an explicit assignment (`IsExplicit = true`, `SourceItemId = null`), preserving the unique constraint `(DriveItemId, UserId)`.
- `roleId` must exist.
- On success: updates existing or inserts explicit row + calls `MaterializeAsync` to propagate to all non-deleted descendants. All changes are committed atomically in a single `SaveChangesAsync`.

**DELETE business rules**:
- Explicit assignment must exist.
- Calls `RemoveInheritedAsync` (removes all inherited descendants) then removes the explicit row.

---

## 7. Business Logic — Key Handler Behaviors

### `LoginCommandHandler`
- Authenticates via `IIdentityService`.
- Issues a JWT (contains `userId` + Identity role claims, e.g. `Admin`).
- Issues a refresh token (stores SHA-256 hash in `RefreshToken` table).

### `RefreshTokenCommandHandler`
- Validates the incoming raw refresh token by hashing it and looking it up.
- Checks expiry and revocation.
- Rotates: revokes the old token, creates a new one, links via `ReplacedByTokenId`.

### `CreateFileCommandHandler`
- Checks parent folder exists and is of type Folder.
- Checks caller owns parent or has `drive.create`.
- Checks no duplicate name in the same parent.
- Uploads the binary to S3 first (key: `files/{driveItemId}/{fileVersionId}`).
- Persists `DriveItem` + `FileVersion` (VersionNumber=1, IsCurrent=true) in one `SaveChangesAsync`.
- On S3 upload success but DB failure: cleans up the S3 object.

### `DeleteDriveItemCommandHandler`
- Soft-deletes: sets `IsDeleted=true`, `DeletedAt`, `UpdatedAt`.
- For folders: recursively queries all active descendants via BFS and marks all descendants soft-deleted with the same timestamp.
- Accessible via unified endpoint `DELETE /api/drive-items/{driveItemId}`.

### `RecoverDriveItemCommandHandler`
- Owner-only: only the drive item owner can recover it (`403 Forbidden` otherwise).
- Checks duplicate name in destination path (ignores deleted items).
- Checks parent folder is active (`409 Conflict` if parent folder is deleted).
- If recovering a folder, automatically recovers cascaded descendants deleted with it.

### `HardDeleteExpiredItemsCommandHandler`
- Identifies items soft-deleted beyond `TrashSettings:RetentionDays`.
- Deletes S3 binaries for file versions via `IFileStorage.DeleteAsync`.
- Deletes items in topological leaf-first order to satisfy PostgreSQL FK restrict constraints.

### `AssignRoleCommandHandler`
- Owner-only, no self-assign, no duplicate explicit assignment.
- Inserts explicit `DriveItemRoleAssignment`.
- Calls `PermissionMaterializer.MaterializeAsync` to propagate to descendants.

### `RemoveAssignmentCommandHandler`
- Verifies explicit assignment exists.
- Calls `RemoveInheritedAsync` (cascades to descendants).
- Removes explicit row + `SaveChangesAsync`.

### `UpdateAssignmentCommandHandler`
- Updates the explicit row's `RoleId`.
- Calls `RemoveInheritedAsync` then `MaterializeAsync` to re-propagate with new role.

### `RenameDriveItemCommandHandler`
- Owner-only: caller must own the item (`403 Forbidden` otherwise).
- Validates name (not empty, no path delimiters, max 255 chars).
- Checks duplicate name in the same parent (ignoring the current item via `IsDuplicateNameAsync(..., excludeItemId: id)`).
- Updates item name and `UpdatedAt`.

### `MoveDriveItemCommandHandler`
- Owner-only: caller must own the item (`403 Forbidden` otherwise).
- If destination folder is specified, destination must exist, be a folder, and be owned by the caller (`403 Forbidden` otherwise).
- Cycle detection: destination cannot be the item itself or any of its descendants (`409 Conflict`).
- Checks for duplicate name in the destination folder (`409 Conflict`).
- Synchronizes role assignments:
  - Preserves explicit assignments (`IsExplicit = true`) on item and descendants.
  - Purges all old inherited assignments (`IsExplicit = false`) on item and descendants via `RemoveInheritedByItemIdsAsync`.
  - If moved to a folder, materializes destination folder roles onto the item and descendants (skipping explicit holders and item owners).
- Updates `ParentId` and `UpdatedAt`.

### `CopyDriveItemCommandHandler`
- Files only: folders cannot be copied (`400 Bad Request`).
- Permission check: caller must own the item or have `drive.read` or `drive.copy`.
- Destination folder must be owned by the caller (or `null` for root).
- Naming strategy: appends `_copy` before file extension. If name is already taken in destination, increments index `_copy(1)`, `_copy(2)`, etc.
- S3 server-side binary duplication: duplicates active file version binary using `IFileStorage.CopyAsync` (`CopyObjectAsync` within S3) into key `files/{newItemId}/{newVersionId}`.
- Persists new `DriveItem` and initial `FileVersion` atomically.

### `HardDeleteDriveItemCommandHandler`
- Item must be soft-deleted (`IsDeleted = true`).
- Caller must own the item or own its parent folder (`403 Forbidden` otherwise).
- Queries all soft-deleted descendants recursively.
- Deletes S3 binaries for all versions of the item and descendants.
- Deletes items in leaf-first order to satisfy FK restrict constraints.

### `EmptyTrashCommandHandler`
- Queries all soft-deleted items owned by the caller.
- Collects and deletes all S3 version binaries for those items.
- Deletes database items leaf-first.

### `UploadAvatarCommandHandler`
- Validates image file presence and content type.
- Uploads avatar to S3 key `avatars/{userId}/{Guid}.{ext}`.
- Deletes previous avatar S3 object if user already had an avatar stored in S3.
- Updates `ApplicationUser.AvatarUrl` via `IUserManagementService.UpdateAvatarAsync`.

### `SearchUsersQueryHandler`
- Searches users where email or display name matches the query string (case-insensitive, min 2 chars).
- Returns up to 20 `UserResult` records.

### `GetCurrentUserQueryHandler`
- Reads `userId` from current user claims.
- Returns `CurrentUserResult` with `id`, `email`, `displayName`, `avatarUrl`.

### `DeleteRoleCommandHandler` (Admin)
- First removes **all** `DriveItemRoleAssignment` rows referencing the role (`RemoveAsync` + `SaveChangesAsync`).
- Then calls `IRoleService.DeleteRoleAsync` (which deletes the Identity role + cascades `IdentityRoleClaims`).

---

## 8. Seed Data (Development)

Runs automatically on startup via `Seeder.cs`.

### Accounts

| Email | Password | Role |
|---|---|---|
| `admin@drive` | `Admin123!` | `Admin` (Identity role) |
| `owner@test` | `Password123!` | No system role — owns seed items |
| `viewer@test` | `Password123!` | No system role — has Viewer assignment on Shared Folder |
| `editor@test` | `Password123!` | No system role — has Editor assignment on Shared Folder |
| `noaccess@test` | `Password123!` | No assignments |

### Roles

| Name | Claims |
|---|---|
| `Admin` | No drive claims (system admin only) |
| `Viewer` | `drive.read`, `drive.download` |
| `Editor` | All 7 `drive.*` |

### Drive Items (owned by `owner@test`)

- **My Folder** (root folder)
  - **Nested Folder** (child of My Folder)
    - **Nested File.txt**
  - **My File.txt**
- **Shared Folder** (root folder) — `viewer@test` has Viewer, `editor@test` has Editor
  - **Seed Shared File A.txt** (inherits assignments from Shared Folder)
  - **Seed Shared File B.pdf** (inherits assignments from Shared Folder)

---

## 9. Key Application Interfaces

| Interface | Implemented by | Purpose |
|---|---|---|
| `IRepository<T>` | `Repository<T>` (EF Core) | `ListAsync`, `CountAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `AddAsync`, `Update`, `RemoveAsync`, `SaveChangesAsync` |
| `ICurrentUserService` | `CurrentUserService` | Reads `userId` from JWT claims in `HttpContext` |
| `IIdentityService` | `IdentityService` | Login (validate credentials, return roles), Register |
| `IRoleService` | `RoleService` | CRUD roles + add/remove permission claims via `RoleManager` |
| `IUserManagementService` | `UserManagementService` | List/get/delete users, `SearchUsersAsync`, `GetUserByEmailAsync`, `UpdateAvatarAsync` |
| `IJwtTokenService` | `JwtTokenService` | Generate access token JWT containing `userId` and role claims |
| `IRefreshTokenService` | `RefreshTokenService` | Create, validate, and revoke refresh tokens (by user or token) |
| `IPermissionService` | `PermissionService` | `HasPermissionAsync(userId, driveItemId, permission)` — owner shortcut + DB join |
| `IPermissionMaterializer` | `PermissionMaterializer` | `MaterializeAsync(driveItemId, userId, roleId, createdBy)` + `RemoveInheritedAsync(sourceItemId, userId)` |
| `IFileStorage` | `S3FileStorage` | `UploadAsync`, `DeleteAsync`, `CopyAsync`, `GetFileUrl`, `BucketName` |
| `IDriveItemRepository` | `DriveItemRepository` | `ListFolderItemsAsync`, `IsDuplicateNameAsync` (with `excludeItemId`), `GetDeletedDescendantsAsync`, `GetUserDeletedItemsAsync`, `GetWithVersionsAsync` |
| `IDriveItemRoleAssignmentRepository` | `DriveItemRoleAssignmentRepository` | `GetAssignmentAsync`, `RemoveInheritedByItemIdsAsync` |
| `ISharedDriveItemQuery` | `SharedDriveItemQuery` | `ListSharedAsync` / `CountSharedAsync` — items where caller has explicit assignment |

---

## 10. Known Deferred Issues (Next Version)

| # | Issue | Location |
|---|---|---|
| #5 | `UpdateAssignment` re-materialization does not update an inherited row that the item itself received from a grandparent | `UpdateAssignmentCommandHandler` |
| #7 | All handlers return `bool` — cannot distinguish 401/403/404 at the controller level | All handlers — needs `ErrorOr`/`Result<T>` migration |

---

## 11. Observability Architecture (Metrics, Logs, Traces)

The system integrates a full Grafana observability stack with bidirectional trace-log correlation:

### 11.1 Components & Protocols
1. **Metrics (Prometheus)**: `prometheus-net` exposes `/metrics` on `Drive.Api`, scraped by Prometheus on port `9090`.
2. **Logs (Loki)**: `Serilog.AspNetCore` + `Serilog.Sinks.Grafana.Loki` sends structured JSON logs to Loki on port `3100`, tagged with `app="drive-api"`, `environment`, and automatically enriched with `TraceId` and `SpanId` via `Serilog.Enrichers.Span`.
3. **Traces (Tempo)**: `OpenTelemetry` instruments incoming HTTP requests, HTTP client calls, and PostgreSQL database queries (`Npgsql.OpenTelemetry`), pushing traces via OTLP gRPC to Tempo on port `4317`.
4. **Grafana**: Web console on port `3000` with pre-configured datasources and correlation links:
   - **Log → Trace**: Loki derived fields parse `TraceId` and provide a 1-click link to the Tempo waterfall.
   - **Trace → Log**: Tempo `tracesToLogsV2` allows clicking any trace span to view matching Loki logs.
   - **Dashboard Creation**: Starts blank for custom creation; see [`Monitoring/grafana_dashboard_gui_guide.md`](../Monitoring/grafana_dashboard_gui_guide.md).

