OJT_Project/ (git repository root)
├── .github/ (empty – no workflow files yet)
├── .gitignore (standard .NET gitignore)
├── noteproject.txt (dev notes)
└── Drive/ (the .NET solution)
├── Drive.slnx (solution: /src + /tests)
├── .vscode/
│ └── settings.json (empty VS Code settings)
├── Drive.Api/ (ASP.NET Core Web API layer)
│ ├── Drive.Api.csproj
│ ├── Drive.Api.csproj.user (per-user debug profile)
│ ├── Program.cs (app entry point / composition root)
│ ├── appsettings.json (DB connection, JWT config, logging)
│ ├── appsettings.Development.json (dev logging overrides)
│ ├── Drive.Api.http (REST Client scratch file)
│ ├── Properties/
│ │ └── launchSettings.json (http/https launch profiles)
│ ├── Mapping/
│ │ └── ApiMappingProfile.cs (AutoMapper: results → API responses)
│ └── Features/
│ ├── Auth/
│ │ ├── Login/
│ │ │ ├── LoginController.cs (POST api/auth/login)
│ │ │ ├── LoginRequest.cs
│ │ │ └── LoginResponse.cs
│ │ ├── Logout/
│ │ │ ├── LogoutController.cs (POST api/auth/logout)
│ │ │ └── LogoutRequest.cs
│ │ ├── RefreshToken/
│ │ │ ├── RefreshTokenController.cs (POST api/auth/refresh)
│ │ │ ├── RefreshTokenRequest.cs
│ │ │ └── TokenResponse.cs
│ │ └── Register/
│ │ ├── RegisterController.cs (POST api/auth/register)
│ │ ├── RegisterRequest.cs
│ │ └── RegisterResponse.cs
│ └── DriveItems/
│ ├── CreateFolder/
│ │ ├── CreateFolderController.cs (POST api/drive-items/folders)
│ │ └── CreateFolderRequest.cs
│ └── List/
│ ├── ListDriveItemsController.cs (GET api/drive-items)
│ ├── DriveItemResponse.cs
│ └── PagedListDriveItemsResponse.cs
├── Drive.Application/ (application/use-case layer)
│ ├── Drive.Application.csproj
│ ├── DependencyInjection.cs (AddApplication: MediatR, validators, AutoMapper)
│ ├── Common/
│ │ ├── Authorization/
│ │ │ └── Permissions.cs (drive.\* permission constants)
│ │ ├── Behaviors/
│ │ │ └── ValidationBehavior.cs (MediatR pipeline: FluentValidation)
│ │ ├── Interfaces/
│ │ │ ├── ICurrentUserService.cs
│ │ │ ├── IDriveItemAccessQuery.cs
│ │ │ ├── IDriveItemAuthorizationService.cs
│ │ │ ├── IIdentityService.cs
│ │ │ ├── IJwtTokenService.cs
│ │ │ ├── IPermissionMaterializer.cs
│ │ │ ├── IPermissionService.cs
│ │ │ ├── IRefreshTokenService.cs
│ │ │ └── IRepository.cs (generic repository contract)
│ │ └── Models/
│ │ ├── PagedResult.cs
│ │ └── Authentication/
│ │ ├── IdentityRegistrationResult.cs
│ │ └── RefreshTokenRotationResult.cs
│ ├── Mapping/
│ │ └── MappingProfile.cs (AutoMapper: entities → results)
│ └── Features/
│ ├── Auth/
│ │ ├── Models/
│ │ │ ├── AuthTokenResult.cs
│ │ │ └── RegisterResult.cs
│ │ └── Commands/
│ │ ├── Login/
│ │ │ ├── LoginCommand.cs
│ │ │ ├── LoginCommandHandler.cs
│ │ │ └── LoginCommandValidator.cs
│ │ ├── Logout/
│ │ │ ├── LogoutCommand.cs
│ │ │ ├── LogoutCommandHandler.cs
│ │ │ └── LogoutCommandValidator.cs
│ │ ├── RefreshToken/
│ │ │ ├── RefreshTokenCommand.cs
│ │ │ ├── RefreshTokenCommandHandler.cs
│ │ │ └── RefreshTokenCommandValidator.cs
│ │ └── Register/
│ │ ├── RegisterCommand.cs
│ │ ├── RegisterCommandHandler.cs
│ │ └── RegisterCommandValidator.cs
│ └── DriveItems/
│ ├── ListDriveItemsScope.cs (Owned | Accessible enum)
│ ├── Models/
│ │ └── DriveItemResult.cs
│ ├── Commands/CreateFolder/
│ │ ├── CreateFolderCommand.cs
│ │ ├── CreateFolderCommandHandler.cs
│ │ └── CreateFolderCommandValidator.cs
│ └── Queries/ListDriveItems/
│ ├── ListDriveItemsQuery.cs
│ ├── ListDriveItemsQueryHandler.cs
│ └── ListDriveItemsQueryValidator.cs
├── Drive.Domain/ (domain layer – no dependencies)
│ ├── Drive.Domain.csproj
│ ├── Entities/
│ │ ├── DriveItem.cs (file/folder aggregate root)
│ │ ├── DriveItemRoleAssignment.cs (per-item permission assignment)
│ │ ├── FileVersion.cs (S3-backed file version record)
│ │ └── RefreshToken.cs (refresh token entity)
│ └── Enums/
│ └── DriveItemType.cs (File = 1, Folder = 2)
├── Drive.Infrastructure/ (infrastructure layer)
│ ├── Drive.Infrastructure.csproj
│ ├── DependencyInjection.cs (AddInfrastructure: DbContext, Identity, JWT…)
│ ├── Authentication/
│ │ ├── CurrentUserService.cs (ICurrentUserService via HttpContext)
│ │ ├── JwtOptions.cs (bound to "Jwt" config section)
│ │ ├── JwtTokenService.cs (issues signed HS256 access tokens)
│ │ ├── PermissionMaterializer.cs (propagates inherited role assignments) [ns: …Authorization]
│ │ ├── PermissionService.cs (owner-or-assignment permission check) [ns: …Authorization]
│ │ └── RefreshTokenService.cs (create/validate/rotate/revoke tokens)
│ ├── Identity/
│ │ ├── ApplicationUser.cs (IdentityUser<Guid> + DisplayName/AvatarUrl)
│ │ ├── IdentitySeeder.cs (static Viewer/Editor role seeder)
│ │ └── IdentityService.cs (register/authenticate via UserManager)
│ ├── Persistence/
│ │ ├── DriveDbContext.cs (IdentityDbContext + domain DbSets)
│ │ ├── Repository.cs (generic EF Core IRepository<> impl)
│ │ ├── DriveItemAccessQuery.cs (permission-aware item listing query)
│ │ ├── Configurations/
│ │ │ ├── ApplicationUserConfiguration.cs
│ │ │ ├── DriveItemConfiguration.cs (constraints, indexes, soft delete)
│ │ │ ├── DriveItemRoleAssignmentConfiguration.cs
│ │ │ ├── FileVersionConfiguration.cs
│ │ │ └── RefreshTokenConfiguration.cs
│ │ └── Migrations/
│ │ ├── 20260811062120_InitialCreate.cs (+ .Designer.cs)
│ │ ├── 20260811092814_RemoveDriveItemPermission.cs (+ .Designer.cs)
│ │ ├── 20260811094035_AddIdentityForeignKeys.cs (+ .Designer.cs)
│ │ ├── 20260812021355_AddDriveItemRoleAssignments.cs (+ .Designer.cs)
│ │ ├── 20260812062412_AddMaterializedPermissionMetadata.cs (+ .Designer.cs)
│ │ └── DriveDbContextModelSnapshot.cs
│ └── Seeding/
│ └── Seeder.cs (startup seed: roles, users, items, permissions)
└── Drive.Tests/ (xUnit test project – no test files yet)
└── Drive.Tests.csproj

(bin/, obj/, .vs/ build artifacts omitted)
