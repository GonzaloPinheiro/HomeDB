using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IBackupService
    {
        /// <summary>
        /// Lanza el proceso de backup diario
        /// </summary>
        Task<BackupAuditEntry> RunDailyBackupAsync(CancellationToken cToken);

        /// <summary>
        /// Lanza un backup para el nivel indicado, evitando solaparse con uno ya en curso para ese mismo nivel
        /// </summary>
        Task<BackupAuditEntry> TriggerBackupAsync(BackupLevel level, CancellationToken cToken);
    }
}
