using HomeDB.Domain.Entities;
using System.Security.AccessControl;

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
        /// 
        /// </summary>
        Task<(IEnumerable<AuditLogEntry> Items, int TotalCount)> GetAuditLogsAsync(int pageNumber, int pageSize,
                                                DateTimeOffset? from, DateTimeOffset? to,
                                                int? userId, string? username, string? action,
                                                string? resourceType,
                                                CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}