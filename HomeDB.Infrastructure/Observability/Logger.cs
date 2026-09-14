using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace HomeDB.Infrastructure.Observability
{
    public class Logger
    {
        //Variables y ojetos
        private readonly ILogQueue _logQueue;
        private readonly ILogEntryRepository _logEntryRepository;
        private readonly LogFailureFileSink _failureSink;

        #region Constructores
        public Logger(ILogEntryRepository logEntryRepository, ILogQueue logQueue, LogFailureFileSink failureSink)
        {
            _logEntryRepository = logEntryRepository ?? throw new ArgumentNullException(nameof(logEntryRepository));
            _logQueue = logQueue ?? throw new ArgumentNullException(nameof(logQueue));
            _failureSink = failureSink ?? throw new ArgumentNullException(nameof(failureSink));
        }
        #endregion

        #region Métodos públicos


        /// <summary>
        /// Encola el log para persistencia asíncrona en background.
        /// </summary>
        /// <param name="entry">Entrada de log (POCO)</param>
        public async Task AddAsync(LogEntry entry)
        {
            if (entry == null)
            {
                //No hay LogEntry que adjuntar, pero se deja constancia de que se perdió un log por esta causa.
                await _failureSink.WriteAsync(LogFailureType.NullEntry, entry, null, CancellationToken.None).ConfigureAwait(false);

                return;
            }

            // Calcular elapsed automáticamente si no viene ya seteado
            if (entry.DurationMs == 0 && OperationLogScope.CurrentStartTime != DateTimeOffset.MinValue)
                entry.DurationMs = Convert.ToInt64((DateTimeOffset.UtcNow - OperationLogScope.CurrentStartTime).TotalMilliseconds);

            // Delegar al ILogQueue (rápido: encola y devuelve)
            await _logQueue.EnqueueAsync(entry).ConfigureAwait(false);
        }

        /// <summary>
        /// Comienza un scope de operación que registra entrada y, al disponer, registra salida con duración.
        /// </summary>
        public OperationLogScope BeginScope(string source, string operation, string? correlationId = null,
                                            string? userId = null)
        {
            OperationLogScope scope = new OperationLogScope(this, source, operation, correlationId, userId);
            return scope;
        }

        /// <summary>
        /// Registra un log dentro de la operación actual (requiere un OperationLogScope activo).
        /// Source, Operation, CorrelationId y UserId se toman automáticamente del contexto del scope.
        /// </summary>
        /// <param name="level">Nivel de severidad del log.</param>
        /// <param name="message">Mensaje del log.</param>
        /// <param name="exception">Excepción asociada, si aplica.</param>
        /// <param name="metadata">
        /// Objeto opcional con datos adicionales, se serializa a MetadataJson.
        /// </param>
        public Task AddAsync(LogLevel level, string message, Exception? exception = null, object? metadata = null)
        {
            LogEntry entry = new LogEntry
            {
                Level = level.ToString(),
                Source = OperationLogScope.CurrentSource,
                Operation = OperationLogScope.CurrentOperation,
                Message = message,
                CorrelationId = OperationLogScope.CurrentCorrelationId ?? string.Empty,
                UserId = OperationLogScope.CurrentUserId,
                Exception = exception?.ToString() ?? string.Empty,
                MetadataJson = metadata is null
                    ? string.Empty
                    : JsonSerializer.Serialize(metadata)
            };

            return AddAsync(entry);
        }
        #endregion
    }
}