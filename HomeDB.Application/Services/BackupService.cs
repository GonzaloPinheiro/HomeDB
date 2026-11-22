using HomeDB.Application.DTOs;
using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class BackupService : IBackupService
    {
        //Variables y objetos globales
        private readonly IBackupAuditRepository _backupAuditRepository;
        private readonly IBackupProcessService _backupProcessService;
        private readonly DatabaseOptions _databaseOptions;
        private readonly IStorageLocationResolver _locationResolver;
        private readonly IStorageAvailabilityService _availabilityService;

        //Constructores
        public BackupService(IBackupAuditRepository backupAuditRepository, IBackupProcessService backupProcessService,
                             IOptions<DatabaseOptions> databaseOptions, IStorageLocationResolver locationResolver,
                             IStorageAvailabilityService availabilityService)
        {
            _backupAuditRepository = backupAuditRepository;
            _backupProcessService = backupProcessService;
            _databaseOptions = databaseOptions.Value;
            _locationResolver = locationResolver;
            _availabilityService = availabilityService;
        }

        //Lanza el proceso de backup diario
        public async Task<BackupAuditEntry> RunDailyBackupAsync(CancellationToken cToken)
        {
            //Comprobar los discos ANTES de tocar nada (registro de auditoría, rotación).
            IReadOnlyCollection<string> sourceLocationKeys = _locationResolver.GetBackupLocationKeys();
            _availabilityService.EnsureAvailable(_locationResolver.ResolveDefaultDiskKey(LocationUsage.Backups), forWrite: true);

            //Comprobar que cada ubicación de backup esté disponible para lectura, lanzando excepción si alguna no lo está
            foreach (string locationKey in sourceLocationKeys)
                _availabilityService.EnsureAvailable(_locationResolver.ResolveDiskKey(locationKey), forWrite: false);

            //Rutas del directorio actual y anterior del backup diario, dentro de la ubicación de backups
            string dailyDirectory = Path.Combine(_locationResolver.ResolveDefaultBasePath(LocationUsage.Backups), "daily");
            string currentPath = Path.Combine(dailyDirectory, "backup_actual");
            string previousPath = Path.Combine(dailyDirectory, "backup_anterior");

            //Marcar como eliminado el registro de auditoría activo más antiguo (si existe), ya que su backup físico va a ser reemplazado
            BackupAuditEntry? obsoleteEntry = await _backupAuditRepository.GetOldestActiveAsync(BackupLevel.Daily, cToken, false);
            if (obsoleteEntry != null)
            {
                obsoleteEntry.DeletedAt = DateTime.UtcNow;
                obsoleteEntry.BackupPath = null;
            }

            //Rotar el backup anterior: el actual pasa a ser el anterior
            if (Directory.Exists(previousPath))
                Directory.Delete(previousPath, true);
            if (Directory.Exists(currentPath))
                Directory.Move(currentPath, previousPath);

            //Crear el directorio del backup actual: contendrá una subcarpeta por ubicación y el volcado de la base de datos
            Directory.CreateDirectory(currentPath);

            //Crear el nuevo registro de auditoría en estado "en curso"
            BackupAuditEntry newEntry = new BackupAuditEntry
            {
                Level = BackupLevel.Daily,
                StartedAt = DateTime.UtcNow,
                Status = BackupStatus.Running,
                BackupPath = currentPath
            };

            //Guardar el registro de auditoría en la base de datos antes de iniciar el proceso de backup
            await _backupAuditRepository.AddAsync(newEntry, cToken);
            await _backupAuditRepository.SaveChangesAsync(cToken);

            try
            {
                //Copiar los archivos de cada ubicación con rsync y capturar el resultado conjunto
                BackupProcessResult rsyncResult = await RunRsyncForLocationsAsync(sourceLocationKeys, currentPath, previousPath, cToken);

                //Volcar la base de datos solo si el rsync fue exitoso, reutilizando la misma cadena de conexión que el resto de la aplicación
                BackupProcessResult pgDumpResult = rsyncResult.Success //Exito
                    ? await _backupProcessService.RunPgDumpAsync(
                        _databaseOptions.PostgreSQL_HomeDB,
                        Path.Combine(currentPath, "database.dump"), cToken)
                    : new BackupProcessResult(false, -1, 0, "Rsync falló, no se ejecutó pg_dump"); //Fallo

                //Actualizar el registro de auditoría con el resultado final del backup
                newEntry.CompletedAt = DateTime.UtcNow;
                newEntry.Status = rsyncResult.Success && pgDumpResult.Success
                    ? BackupStatus.Success
                    : BackupStatus.Failed;
                newEntry.FilesBackedUpSizeBytes = rsyncResult.OutputSizeBytes;
                newEntry.DatabaseDumpSizeBytes = pgDumpResult.OutputSizeBytes;
                newEntry.ErrorMessage = rsyncResult.ErrorMessage
                    ?? pgDumpResult.ErrorMessage;
            }
            catch (Exception ex)
            {
                //Si algo revienta antes de tener un resultado final (falta de configuración, fallo de IO, cancelación...),
                //el entry no puede quedar en Running para siempre: se marca como fallido y se relanza la excepción original
                newEntry.CompletedAt = DateTime.UtcNow;
                newEntry.Status = BackupStatus.Failed;
                newEntry.ErrorMessage = ex.Message;

                //Se guarda con un token nuevo porque cToken puede venir ya cancelado
                await _backupAuditRepository.SaveChangesAsync(CancellationToken.None);
                throw;
            }

            //Persistir los cambios en la base de datos
            await _backupAuditRepository.SaveChangesAsync(cToken);

            return newEntry;
        }

        //Copia con rsync cada ubicación marcada para backup en su propia subcarpeta (backup_actual/<clave de ubicación>).
        //Se detiene en el primer fallo, que se devuelve indicando la ubicación; si todo va bien devuelve el tamaño total.
        private async Task<BackupProcessResult> RunRsyncForLocationsAsync(IReadOnlyCollection<string> locationKeys, string currentPath,
                                                                          string previousPath, CancellationToken cToken)
        {
            long totalSizeBytes = 0;

            //Para cada ubicación marcada para backup, se lanza un rsync que copia su contenido.
            foreach (string locationKey in locationKeys)
            {
                //Se obtiene la ruta base de la ubicación
                string sourcePath = _locationResolver.ResolveBasePath(locationKey);

                //Una ubicación que todavía no ha recibido ningún archivo no tiene carpeta: no hay nada que copiar
                if (!Directory.Exists(sourcePath))
                    continue;

                string locationCurrentPath = Path.Combine(currentPath, locationKey);
                string locationPreviousPath = Path.Combine(previousPath, locationKey);

                //Reutilizar el backup anterior de esta ubicación como link-dest si existe, para ahorrar espacio
                string? linkDestPath = Directory.Exists(locationPreviousPath)
                    ? locationPreviousPath
                    : null;

                //La barra final hace que rsync copie el contenido de la carpeta y no la carpeta misma
                string sourceWithSeparator = Path.TrimEndingDirectorySeparator(sourcePath) + Path.DirectorySeparatorChar;

                //Lanzar rsync para copiar la ubicación, usando link-dest si hay un backup anterior
                BackupProcessResult result = await _backupProcessService.RunRsyncAsync(sourceWithSeparator, locationCurrentPath, linkDestPath, cToken);

                //Si rsync falla, devolver el error indicando la ubicación que falló
                if (!result.Success)
                    return new BackupProcessResult(false, result.ExitCode, 0, $"[{locationKey}] {result.ErrorMessage}");

                totalSizeBytes += result.OutputSizeBytes;
            }

            return new BackupProcessResult(true, 0, totalSizeBytes, null);
        }

        //Lanza manualmente un backup para el nivel indicado, evitando solaparse con uno ya en curso para ese mismo nivel
        public async Task<BackupAuditEntry> TriggerBackupAsync(BackupLevel level, CancellationToken cToken)
        {
            //Comprobar que no haya ya un backup en curso para el nivel indicado antes de lanzar uno nuevo
            BackupAuditEntry? latestActiveEntry = await _backupAuditRepository.GetLatestActiveAsync(level, cToken);
            if (latestActiveEntry?.Status == BackupStatus.Running)
                throw new BackupAlreadyRunningException(level);

            //Despachar al proceso correspondiente según el nivel de backup solicitado
            return level switch
            {
                BackupLevel.Daily => await RunDailyBackupAsync(cToken),
                _ => throw new BackupLevelNotSupportedException(level)
            };
        }

        //Devuelve los registros de auditoría de backup según los filtros proporcionados en el DTO
        public async Task<GetBackupHistoryResponseDto> GetHistoryAsync(GetBackupHistoryRequestDto dto, CancellationToken cToken)
        {
            //Obtiene el historial de backups paginado, opcionalmente filtrado por nivel
            (IEnumerable<BackupAuditEntry> items, int totalCount) = await _backupAuditRepository.GetHistoryAsync(
                dto.Level, dto.Page, dto.PageSize, cToken);

            //Devuelve un DTO con los resultados y la información de paginación
            return new GetBackupHistoryResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = dto.Page,
                PageSize = dto.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / dto.PageSize)
            };
        }
    }
}