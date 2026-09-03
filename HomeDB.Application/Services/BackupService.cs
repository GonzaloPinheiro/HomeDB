using HomeDB.Application.Options;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class BackupService : IBackupService
    {
        //Variables y objetos globales
        private readonly IBackupAuditRepository _backupAuditRepository;
        private readonly IBackupProcessService _backupProcessService;
        private readonly IConfiguration _configuration;
        private readonly BackupOptions _backupOptions;

        //Constructores
        public BackupService(IBackupAuditRepository backupAuditRepository, IBackupProcessService backupProcessService,
                             IConfiguration configuration, IOptions<BackupOptions> backupOptions)
        {
            _backupAuditRepository = backupAuditRepository;
            _backupProcessService = backupProcessService;
            _configuration = configuration;
            _backupOptions = backupOptions.Value;
        }

        //Lanza el proceso de backup diario
        public async Task RunDailyBackupAsync(CancellationToken cToken)
        {
            //Rutas del directorio actual y anterior del backup diario
            string currentPath = Path.Combine(_backupOptions.DailyDirectory, "backup_actual");
            string previousPath = Path.Combine(_backupOptions.DailyDirectory, "backup_anterior");

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

            //Copiar los archivos con rsync, reutilizando el backup anterior como link-dest si existe para ahorrar espacio
            string? linkDestPath = Directory.Exists(previousPath) 
                ? previousPath 
                : null;

            //Ejecutar el proceso de rsync y capturar el resultado
            BackupProcessResult rsyncResult = await _backupProcessService.RunRsyncAsync(_backupOptions.SourceDirectory, currentPath, linkDestPath, cToken);

            //Volcar la base de datos solo si el rsync fue exitoso, reutilizando la misma cadena de conexión que el resto de la aplicación
            BackupProcessResult pgDumpResult = rsyncResult.Success
                ? await _backupProcessService.RunPgDumpAsync(
                    _configuration.GetConnectionString("PostgreSQL_HomeDB") 
                        ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL_HomeDB no configurado"),
                    Path.Combine(currentPath, "database.dump"), cToken)
                : new BackupProcessResult(false, -1, 0, "Rsync falló, no se ejecutó pg_dump");

            //Actualizar el registro de auditoría con el resultado final del backup
            newEntry.CompletedAt = DateTime.UtcNow;
            newEntry.Status = rsyncResult.Success && pgDumpResult.Success ? BackupStatus.Success : BackupStatus.Failed;
            newEntry.FilesBackedUpSizeBytes = rsyncResult.OutputSizeBytes;
            newEntry.DatabaseDumpSizeBytes = pgDumpResult.OutputSizeBytes;
            newEntry.ErrorMessage = rsyncResult.ErrorMessage ?? pgDumpResult.ErrorMessage;

            //Persistir los cambios en la base de datos
            await _backupAuditRepository.SaveChangesAsync(cToken);
        }
    }
}