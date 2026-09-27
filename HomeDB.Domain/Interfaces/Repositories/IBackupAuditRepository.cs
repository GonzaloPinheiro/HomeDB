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
        /// Devuelve la fecha y hora del último backup exitoso completado para un nivel de backup específico.
        /// </summary>
        Task<DateTime?> GetLastSuccessfulCompletedAtAsync(BackupLevel level, CancellationToken cToken);

        /// <summary>
        /// Devuelve el registro del backup más reciente que aún está activo, para un nivel de backup específico.
        /// </summary>
        Task<BackupAuditEntry?> GetLatestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve un listado paginado de los registros de auditoría de backup para un nivel de backup específico.
        /// </summary>
        Task<(IEnumerable<BackupAuditEntry> Items, int TotalCount)> GetHistoryAsync(BackupLevel? level, int pageNumber, int pageSize, CancellationToken cToken);

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