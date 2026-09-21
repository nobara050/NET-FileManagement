# Drive API

A lightweight RESTful backend service for file and folder management with role-based access control and item sharing, built with .NET 10.

---

## Tech Stack

- **Runtime & Framework:** .NET 10 (C#), ASP.NET Core Web API
- **Architecture:** Clean Architecture (API, Application, Domain, Infrastructure), CQRS with MediatR
- **Database:** PostgreSQL with Entity Framework Core
- **Object Storage:** AWS S3 (mocked via LocalStack for local development)
- **Authentication:** JWT Bearer & Refresh Token flow, ASP.NET Core Identity
- **Observability:** OpenTelemetry, Prometheus, Grafana Loki, Grafana Tempo
- **Containerization:** Docker & Docker Compose
- **Testing:** xUnit, FluentAssertions, Moq

---

## Quick Start

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/)

### 2. Run Infrastructure
Start all dependencies (PostgreSQL, LocalStack S3, Prometheus, Loki, Tempo, Grafana):

```bash
docker compose up -d
```

### 3. Setup Environment
```bash
cp .env.example .env
```

### 4. Run API
```bash
dotnet run --project Drive/Drive.Api
```

API will be accessible at: `https://localhost:5213` 
Swagger UI: `https://localhost:5213/swagger`

---

## API Endpoints

### Authentication (`/api/auth`)
| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register a new user account |
| `POST` | `/api/auth/login` | Login and receive access & refresh tokens |
| `POST` | `/api/auth/refresh` | Exchange refresh token for a new access token |
| `POST` | `/api/auth/logout` | Revoke current session |
| `GET` | `/api/auth` / `/api/users/me` | Get profile of currently authenticated user |
| `POST` | `/api/auth/avatar` | Upload and update user avatar |

### Drive Items (`/api/drive-items`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/drive-items` | List files and folders (filter by `parentId` or root) |
| `POST` | `/api/drive-items/files` | Upload a new file |
| `POST` | `/api/drive-items/folders` | Create a new folder |
| `PATCH` | `/api/drive-items/rename` | Rename a file or folder |
| `PATCH` | `/api/drive-items/move` | Move a file or folder to another destination |
| `GET` | `/api/drive-items/{id}/download` | Get presigned S3 download URL |
| `GET` | `/api/drive-items/{id}/preview` | Get presigned S3 preview URL |
| `DELETE` | `/api/drive-items/{id}` | Soft delete an item (move to trash) |
| `GET` | `/api/drive-items/deleted` | List items currently in trash |
| `POST` | `/api/drive-items/{id}/recover` | Restore a deleted item from trash |
| `DELETE` | `/api/drive-items/trash/{id}` | Permanently delete an item from trash |
| `DELETE` | `/api/drive-items/trash` | Empty all items from trash |

### Permissions & Sharing (`/api/drive-items/{id}/assignments`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/drive-items/{id}/assignments` | List all role assignments for an item |
| `POST` | `/api/drive-items/{id}/assignments` | Share item / assign role to a user (Viewer/Editor) |
| `PUT` | `/api/drive-items/{id}/assignments/{userId}` | Update role assignment for a user |
| `DELETE` | `/api/drive-items/{id}/assignments/{userId}` | Revoke access for a user |

### Users & Roles (`/api/users`, `/api/roles`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users/search?query={q}` | Search users by username or email |
| `GET` | `/api/users` | List all users (Admin only) |
| `GET` | `/api/users/{userId}` | Get user details (Admin only) |
| `DELETE` | `/api/users/{userId}` | Delete a user (Admin only) |
| `GET` | `/api/roles` | List all roles |
| `POST` | `/api/roles` | Create a new role (Admin only) |
| `PUT` | `/api/roles/{roleId}/name` | Update role name (Admin only) |
| `DELETE` | `/api/roles/{roleId}` | Delete a role (Admin only) |
| `POST` | `/api/roles/{roleId}/claims` | Add claim to a role (Admin only) |
| `DELETE` | `/api/roles/{roleId}/claims/{claimValue}` | Remove claim from a role (Admin only) |

---

## Infrastructure & Port Mappings

| Service | Port / Address | Default Credentials | Description |
|---|---|---|---|
| **Swagger UI** | `https://localhost:5213/swagger` | - | Interactive API documentation |
| **PostgreSQL** | `localhost:5432` | `postgres` / `147575` | Main relational database (`drive`) |
| **LocalStack (S3)** | `http://localhost:4566` | Profile `localstack` (`test`/`test`) | S3 mock bucket (`drive-files`) |
| **Grafana** | `http://localhost:3000` | `admin` / `admin` | Observability dashboard |
| **Prometheus** | `http://localhost:9090` | - | Metrics scraper & engine |
| **Loki** | `http://localhost:3100` | - | Log aggregation sink |
| **Tempo** | `http://localhost:3200` | - | Distributed tracing receiver |

---

## Running Tests

Run all unit and integration tests:

```bash
dotnet test
```
