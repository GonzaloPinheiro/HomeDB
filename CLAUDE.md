# HomeDB

Solución: `HomeDB.slnx`. .NET 8 / ASP.NET Core / Clean Architecture / PostgreSQL + EF Core 8 (snake_case vía EFCore.NamingConventions) / JWT Bearer + refresh tokens propios.
Sin MediatR, FluentValidation, AutoMapper ni Serilog.

## Proyectos y namespaces

- **`HomeDB.Domain`** (`HomeDB.Domain.*`) — `Entities/`, `Interfaces/`, `Exceptions/`, `Common/`
  - Entidades: `User`, `FileItem`, `FolderItem`, `RefreshToken`, `AuditLogEntry`, `Role`, `UserRole`, `LogEntry`
  - Interfaces repositorio: `IUserRepository`, `IFileItemRepository`, `IFolderRepository`, `IRefreshTokenRepository`, `IAuditLogRepository`, `ILogEntryRepository`
- **`HomeDB.Application`** (`HomeDB.Application.*`) — `Services/`, `DTOs/`, `Options/`
  - Servicios: `AuthService`, `FilesService`, `FoldersService`, `AuditService`, `StatisticsService`, `LogsService`, `StorageStatusService`
- **`HomeDB.Infrastructure`** (`HomeDB.Infrastructure.*`) — `Data/`, `Repositories/`, `Migrations/`, `Security/`, `Storage/`, `Observability/`
  - Repos: `UserRepository`, `FileItemRepository`, `FolderRepository`, `AuditLogEntryRepository`, `RefreshTokenRepository`, `LogEntryRepository`¹
- **`HomeDB`** (`HomeDB.*`) — `Controllers/`, `Middlewares/`, `Authorization/`, `DependencyInjection/`, `Common/`, `Program.cs`
  - Controllers: `AuthController`, `FilesController`, `FoldersController`, `AdminController`, `StatisticsController`, `StorageController`, `HealthController`

¹ `LogEntryRepository` usa `IDbContextFactory<AppDbContext>` (no `AppDbContext` directo) porque vive en un background service.

## Respuesta API

`ApiObjResponse<T>` en `HomeDB/Common/`. Usar siempre los factory methods:
```csharp
ApiObjResponse<T>.Success(data)
ApiObjResponse<T>.Failure(ApiErrorCodes.UserNotFound, "mensaje")
```
`ApiErrorCodes` es enum: `FileNotFound=1001`, `FolderNotFound=1002`, `Unauthorized=1003`, `FileTooLarge=1004`, `FolderNotEmpty=1005`, `InvalidCredentials=1006`, `UserAlreadyExists=1007`, `UserNotFound=1008`, `RateLimitExceeded=1009`, `StorageLocationUnavailable=1028`, `InternalError=9999`.

## Roles

`RolesList` enum en `HomeDB.Domain/Common/RolesList.cs` (`Admin=1`, `User=2`).
```csharp
[Authorize(Roles = nameof(RolesList.Admin))]  // AdminController (clase), AuthController.RegisterAsync
[Authorize]  // FilesController, FoldersController, StatisticsController
```

## Capas y dependencias externas

`HomeDB.Application` no debe referenciar paquetes `Microsoft.AspNetCore.*` ni `Microsoft.Extensions.Configuration` — son detalles de la capa web/host, no del dominio de negocio. Única excepción tolerada: `Microsoft.Extensions.Options` / `.Options.DataAnnotations` (agnósticos de host: funcionan igual en web, consola o worker).

Consecuencias de esta regla:
- Autorización basada en módulos (`RequireModuleAttribute`, `ModuleAuthorizationHandler`, `ModuleRequirement`) vive en `HomeDB/Authorization/` (capa web), no en Application. La regla de negocio ("¿tiene el usuario este módulo habilitado?") vive en `UserModulePermissionsService.HasAccessAsync()`.
- Cualquier traducción a un tipo de framework (p. ej. `SameSitePolicy` de dominio → `SameSiteMode` de ASP.NET Core) se hace en un extension method de `HomeDB/Common/`, nunca dentro de Application.
- Si un caso de uso necesita algo que solo ofrece un paquete de framework (tipo de contenido por extensión, cadena de conexión, etc.), Application define la interfaz en `HomeDB.Domain.Interfaces.Services` y `HomeDB.Infrastructure` la implementa (p. ej. `IContentTypeResolver`).

## Almacenamiento multi-ubicación

Config `Storage:Disks[]`: cada disco (`Key`, `Path`, `StartupPolicy` Required/Optional, `MinFreeBytes`) tiene `Locations[]` (`Key`, `Usage`, `IsDefault`, `SubPath`, `IncludeInBackup`, `MaxUsedBytes`). Cada `FileItem` guarda su `LocationKey`. Guía de despliegue en `docs/despliegue-almacenamiento.md`.
- Cada disco lleva en su raíz un fichero `.homedb-location` cuyo contenido es la **clave del disco**. Sin él el disco no está disponible: las operaciones dan 503 con `StorageLocationUnavailable` (`StorageLocationUnavailableException`).
- Todo acceso a ficheros pasa por `IFileStorageService`, que llama a `IStorageAvailabilityService.EnsureAvailable` antes de leer, escribir o borrar. **Nunca `Directory.CreateDirectory` ni escrituras en una ubicación antes de esa comprobación**: con el disco ausente crearían la carpeta en el disco principal.
- Para rutas usar `IStorageLocationResolver` (por uso por defecto o por clave de ubicación), nunca rutas literales.
- Arranque: `ValidateStorageDisksAsync()` (antes de migrar) y `ValidateStoredLocationKeys()` (después), en `HomeDB/DependencyInjection/StorageStartupExtensions.cs`. En ejecución, `StorageAvailabilityBackgroundService` vigila los discos.
- `GET api/storage/locations` (módulo `SystemMonitor`) expone el estado; el monitor de sistema sigue leyendo `/` (disco principal de la Pi).
- En tests de integración un disco debe tener su marcador (ver `HomeDbApiFactory`); `SimulateDiskUnavailable()` lo borra y lo restaura al hacer `Dispose`.

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
