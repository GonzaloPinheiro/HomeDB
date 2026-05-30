using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface ILogEntryRepository
    {
        /// <summary>
        /// Inserta en la base de datos un nuevo registro de logEntry.
        /// </summary>
        Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken);

        /// <summary>
        /// Obtiene una lista de logs filtrados por los parámetros recibidos.
        /// </summary>
        Task<(IEnumerable<LogEntry> Items, int TotalCount)> GetLogsAsync(string? level, string? operation, DateTimeOffset? from, DateTimeOffset? to,
                                                                         string? correlationId, int page, int pageSize, CancellationToken cToken);
        /// <summary>
        /// Obietiene el número de errores y warnings registrados en la última hora y en las últimas 24 horas.
        /// </summary>
        /// <param name="cToken"></param>
        Task<IEnumerable<(string Level, DateTimeOffset TimeStamp)>> GetHealthAsync(CancellationToken cToken);

        /// <summary>
        /// Obtener el resumen de errores por operación en las últimas N horas
        /// </summary>
        Task<IEnumerable<(string Operation, int Count)>> GetErrorSummaryAsync(int hours, CancellationToken cToken);

        /// <summary>
        /// Devuelve una lista de operaciones que han superado un umbral de duración en milisegundos;
        /// </summary>
        Task<IEnumerable<(string Operation, long DurationMs, DateTimeOffset TimeStamp)>> GetSlowOperationsAsync(long thresholdMs, CancellationToken cToken);

    }
}