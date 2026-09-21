# Drive Backend — Developer Guide

> **Drive Clone** — a file-management API with folder-level RBAC, built on ASP.NET Core Clean Architecture + CQRS.

For the complete technical reference (all entities, endpoints, business rules), see [`architecture.md`](./architecture.md).

---

## Quick Start

```bash
# Copy environment file and fill in secrets
cp .env.example .env

# Start PostgreSQL + LocalStack (S3) + monitoring
docker compose up -d

# Run the API
cd Drive
dotnet run --project Drive.Api
```

Swagger UI: `https://localhost:{port}/swagger`  
Seed admin account: `admin@drive` / `Admin123!`

---

## Observability & Monitoring

The repository includes a complete local observability stack managed via Docker Compose:

| Service | Port | Description |
|---|---|---|
| **Grafana** | `3000` | UI Dashboard (`admin` / `admin`). Starts blank; see GUI creation guide |
| **Prometheus** | `9090` | Metrics scraper (scrapes `/metrics` via `prometheus-net`) |
| **Loki** | `3100` | Structured log aggregation (emitted via `Serilog.Sinks.Grafana.Loki`) |
| **Tempo** | `3200` / `4317` | Distributed tracing (OTLP gRPC via OpenTelemetry for HTTP, DB queries, S3) |

- **Step-by-Step GUI Dashboard Guide**: [`Monitoring/grafana_dashboard_gui_guide.md`](../Monitoring/grafana_dashboard_gui_guide.md)


---

## Architecture at a Glance

```
HTTP Request
  │
  ▼
Drive.Api            ← Controllers, Request/Response models, Auth policies
  │  MediatR Send(Command / Query)
  ▼
Drive.Application    ← Handlers, Validators (FluentValidation), Interfaces
  │  Implementations injected via DI
  ▼
Drive.Infrastructure ← EF Core, ASP.NET Identity, JWT, S3, Repository
  │
  ▼
PostgreSQL / S3
```

### Layer responsibilities

| Layer | Project | Depends on |
|---|---|---|
| Domain | `Drive.Domain` | Nothing |
| Application | `Drive.Application` | Domain |
| Infrastructure | `Drive.Infrastructure` | Application + Domain |
| API | `Drive.Api` | Application + Infrastructure |

---

## Authorization Model

Two independent authorization mechanisms coexist:

### 1. System-level (JWT Role Claim)
- The `Admin` policy (`[Authorize(Policy = "Admin")]`) requires the `Admin` role claim inside the JWT.
- Used for `/api/users/*` and most `/api/roles/*` endpoints.

### 2. Item-level (RBAC via `DriveItemRoleAssignment`)
- Any user can be assigned a `Role` (e.g. `Viewer`, `Editor`) on a specific `DriveItem`.
- The item **owner** has implicit full access and can manage assignments.
- Permissions propagate automatically to all descendant items (`PermissionMaterializer`).
- Checked at handler level via `IPermissionService.HasPermissionAsync`.

---

## Endpoint Groups

| Group | Base path | Access |
|---|---|---|
| Auth | `/api/auth` | Login/Register: Public; Logout: Authenticated |
| User Management | `/api/users` | Admin only |
| Role Management | `/api/roles` | GET: any authenticated; rest: Admin only |
| Drive Items | `/api/drive-items` | Any authenticated (ownership/permission checked in handler) |
| Item Assignments | `/api/drive-items/{itemId}/assignments` | Item owner only (checked in handler) |

Full endpoint details with request/response shapes: see **Section 6** of [`architecture.md`](./architecture.md).

---

## Seed Accounts (Development)

| Email | Password | Notes |
|---|---|---|
| `admin@drive` | `Admin123!` | Full admin access |
| `owner@test` | `Password123!` | Owns all seed drive items |
| `viewer@test` | `Password123!` | Viewer role on Shared Folder |
| `editor@test` | `Password123!` | Editor role on Shared Folder |
| `noaccess@test` | `Password123!` | No assignments |

---

## Source Code Reading Order

Follow this order to understand the codebase from the ground up:

### 1 — Domain (data model)
1. [`Drive.Domain/Enums/DriveItemType.cs`](Drive.Domain/Enums/DriveItemType.cs) — `File=1`, `Folder=2`
2. [`Drive.Domain/Entities/DriveItem.cs`](Drive.Domain/Entities/DriveItem.cs) — central entity
3. [`Drive.Domain/Entities/DriveItemRoleAssignment.cs`](Drive.Domain/Entities/DriveItemRoleAssignment.cs) — item-level RBAC
4. [`Drive.Domain/Entities/FileVersion.cs`](Drive.Domain/Entities/FileVersion.cs) — S3 version history
5. [`Drive.Domain/Entities/RefreshToken.cs`](Drive.Domain/Entities/RefreshToken.cs)

### 2 — Application Interfaces (contracts, no implementations)
1. [`Drive.Application/Common/Authorization/Permissions.cs`](Drive.Application/Common/Authorization/Permissions.cs)
2. [`Drive.Application/Common/Interfaces/IRepository.cs`](Drive.Application/Common/Interfaces/IRepository.cs)
3. [`Drive.Application/Common/Interfaces/IPermissionService.cs`](Drive.Application/Common/Interfaces/IPermissionService.cs)
4. [`Drive.Application/Common/Interfaces/IPermissionMaterializer.cs`](Drive.Application/Common/Interfaces/IPermissionMaterializer.cs)
5. [`Drive.Application/Common/Interfaces/IRoleService.cs`](Drive.Application/Common/Interfaces/IRoleService.cs)
6. All other interfaces in `Drive.Application/Common/Interfaces/`

### 3 — Infrastructure (implementations)
1. [`Drive.Infrastructure/Persistence/DriveDbContext.cs`](Drive.Infrastructure/Persistence/DriveDbContext.cs)
2. [`Drive.Infrastructure/Authorization/PermissionService.cs`](Drive.Infrastructure/Authorization/PermissionService.cs)
3. [`Drive.Infrastructure/Authorization/PermissionMaterializer.cs`](Drive.Infrastructure/Authorization/PermissionMaterializer.cs)
4. [`Drive.Infrastructure/Identity/RoleService.cs`](Drive.Infrastructure/Identity/RoleService.cs)
5. [`Drive.Infrastructure/Seeding/Seeder.cs`](Drive.Infrastructure/Seeding/Seeder.cs)

### 4 — Application Handlers (business logic)
1. Auth: `Drive.Application/Features/Auth/Commands/`
2. Drive Items: `Drive.Application/Features/DriveItems/Commands/` and `Queries/`
3. Roles: `Drive.Application/Features/Roles/`
4. Users: `Drive.Application/Features/Users/`

### 5 — API Layer
1. [`Drive.Api/Program.cs`](Drive.Api/Program.cs) — DI, middleware, policies
2. Controllers in `Drive.Api/Features/`
3. [`Drive.Api/Authorization/PermissionAuthorizationHandler.cs`](Drive.Api/Authorization/PermissionAuthorizationHandler.cs)
