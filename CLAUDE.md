# HomeDB

Solución: `HomeDB.slnx`. .NET 8 / ASP.NET Core / Clean Architecture / PostgreSQL + EF Core 8 (snake_case vía EFCore.NamingConventions) / JWT Bearer + refresh tokens propios.
Sin MediatR, FluentValidation, AutoMapper ni Serilog.

## Proyectos y namespaces

- **`HomeDB.Domain`** (`HomeDB.Domain.*`) — `Entities/`, `Interfaces/`, `Exceptions/`, `Common/`
  - Entidades: `User`, `FileItem`, `FolderItem`, `RefreshToken`, `AuditLogEntry`, `Role`, `UserRole`, `LogEntry`
  - Interfaces repositorio: `IUserRepository`, `IFileItemRepository`, `IFolderRepository`, `IRefreshTokenRepository`, `IAuditLogRepository`, `ILogEntryRepository`
- **`HomeDB.Application`** (`HomeDB.Application.*`) — `Services/`, `DTOs/`
  - Servicios: `AuthService`, `FilesService`, `FoldersService`, `AuditService`, `StatisticsService`, `LogsService`
- **`HomeDB.Infrastructure`** (`HomeDB.Infrastructure.*`) — `Data/`, `Repositories/`, `Migrations/`, `Security/`, `Storage/`, `Observability/`
  - Repos: `UserRepository`, `FileItemRepository`, `FolderRepository`, `AuditLogEntryRepository`, `RefreshTokenRepository`, `LogEntryRepository`¹
- **`HomeDB`** (`HomeDB.*`) — `Controllers/`, `Middlewares/`, `DependencyInjection/`, `Common/`, `Program.cs`
  - Controllers: `AuthController`, `FilesController`, `FoldersController`, `AdminController`, `StatisticsController`, `HealthController`

¹ `LogEntryRepository` usa `IDbContextFactory<AppDbContext>` (no `AppDbContext` directo) porque vive en un background service.

## Respuesta API

`ApiObjResponse<T>` en `HomeDB/Common/`. Usar siempre los factory methods:
```csharp
ApiObjResponse<T>.Success(data)
ApiObjResponse<T>.Failure(ApiErrorCodes.UserNotFound, "mensaje")
```
`ApiErrorCodes` es enum: `FileNotFound=1001`, `FolderNotFound=1002`, `Unauthorized=1003`, `FileTooLarge=1004`, `FolderNotEmpty=1005`, `InvalidCredentials=1006`, `UserAlreadyExists=1007`, `UserNotFound=1008`, `RateLimitExceeded=1009`, `InternalError=9999`.

## Roles

`RolesList` enum en `HomeDB.Domain/Common/RolesList.cs` (`Admin=1`, `User=2`).
```csharp
[Authorize(Roles = nameof(RolesList.Admin))]  // AdminController (clase), AuthController.RegisterAsync
[Authorize]  // FilesController, FoldersController, StatisticsController
```

## IDs

Todos los IDs de entidades son `int`. GUIDs solo en `StoredName` (nombre en disco) y `CorrelationId` (logs).

## Logging

Inyectar siempre la clase concreta `Logger` (`HomeDB.Infrastructure.Observability`), nunca `ILogger<T>`. Patrón obligatorio al inicio de cada action method:
```csharp
await using OperationLogScope scope = _logger.BeginScope(
    source: "HomeDB.Controllers.NombreController",
    operation: "NombreMetodoAsync()",
    correlationId: correlationId,
    userId: userId);
```

## Comandos

```bash
dotnet run --project HomeDB/HomeDB.csproj
dotnet publish "HomeDB/HomeDB.csproj" -c Release -o /app/publish --no-restore
docker compose up --build
```
Migraciones: automáticas en startup (`db.Database.Migrate()`). No generar ni aplicar manualmente.

## Convenciones

- **Nunca `var`** — tipos explícitos siempre.
- Campos privados: `_camelCase`.
- Métodos async: sufijo `Async` + `CancellationToken cToken` siempre.
- DTOs: `record` inmutables — `record RegisterDto(string Username, string Email)`.
- Entidades: `class` mutable.
- Controllers: heredan de `ApiControllerBase` — usar `GetUserId()`, `GetCorrelationId()`, `GetIpAddress()`.
- Errores: lanzar excepciones de dominio propias (`UserAlreadyExistsException`, etc.) — `ExceptionHandlerMiddleware` las mapea a HTTP.

## Ignorar

`**/bin/`, `**/obj/`, `HomeDB.Infrastructure/Migrations/`
