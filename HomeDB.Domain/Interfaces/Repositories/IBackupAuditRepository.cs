using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IBackupAuditRepository
    {
        /// <summary>
        /// Devuelve el registro del backup más antiguo que aún está activo, para un nivel de backup específico.
        /// </summary>
        Task<BackupAuditEntry?> GetOldestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Crea un nuevo registro de auditoría de backup en la base de datos.
        /// </summary>
        Task AddAsync(BackupAuditEntry entry, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}