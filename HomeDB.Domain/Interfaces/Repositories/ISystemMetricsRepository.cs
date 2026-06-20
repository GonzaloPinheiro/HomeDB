using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface ISystemMetricsRepository
    {
        /// <summary>
        /// Inserta un registro en la base de datos con la información del sistema (Con SaveChangesAsync interno)
        /// </summary>
        Task InsertAsync(SystemMetricsEntry entry, CancellationToken cToken);

        /// <summary>
        /// Obtiene un historial de registros dentro del margen establecido
        /// </summary>
        Task<IEnumerable<SystemMetricsEntry>> GetRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cToken);

        /// <summary>
        /// Obtiene el último registro
        /// </summary>
        Task<SystemMetricsEntry?> GetLastAsync(CancellationToken cToken);

        /// <summary>
        /// Borra los registros con mas antiguedad del cutoff recibido por parámetro
        /// </summary>
        Task DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cToken);
    }
}