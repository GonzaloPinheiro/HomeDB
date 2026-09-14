using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Inserta una nueva entrada de auditoría en la base de datos.
        /// </summary>
        Task InsertAsync(AuditLogEntry auditLogEntry, CancellationToken cToken);

        /// <summary>
        /// Obtiene una lista de entradas de auditoría filtradas por los parámetros recibidos.
        /// </summary>
        Task<(IEnumerable<AuditLogEntry> Items, int TotalCount)> GetAuditLogsAsync(int pageNumber, int pageSize,
                                                DateTimeOffset? from, DateTimeOffset? to,
                                                int? userId, string? username, string? action,
                                                string? resourceType,
                                                CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}