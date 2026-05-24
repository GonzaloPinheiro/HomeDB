using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Inserta una nueva entrada de auditoría en la base de datos.
        /// </summary>
        /// <param name="auditLogEntry"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task InsertAsync(AuditLogEntry auditLogEntry, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}