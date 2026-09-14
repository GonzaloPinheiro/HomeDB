using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IBackupService
    {
        /// <summary>
        /// Lanza el proceso de backup diario
        /// </summary>
        Task<BackupAuditEntry> RunDailyBackupAsync(CancellationToken cToken);
    }
}
