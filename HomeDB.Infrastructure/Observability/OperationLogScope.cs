using HomeDB.Domain.Entities;

namespace HomeDB.Infrastructure.Observability
{
    // Helper que registra automáticamente "Entrada" al crear y "Salida" al disponer,
    // además calcula la duración. Usa AddAsync del Logger internamente.
    public sealed class OperationLogScope : IAsyncDisposable
    {
        //Variables y objetos
        private static readonly System.Threading.AsyncLocal<string> _currentCorrelationId = new System.Threading.AsyncLocal<string>();
        private static readonly System.Threading.AsyncLocal<DateTimeOffset> _currentStartTime = new System.Threading.AsyncLocal<DateTimeOffset>();
        private readonly Logger _logger = null;
        private readonly string _source = string.Empty;
        private readonly string _operation = string.Empty;
        private readonly string _correlationId = string.Empty;
        private readonly string _userId = string.Empty;
        private readonly DateTimeOffset _start = DateTimeOffset.MinValue;

        public static string CurrentCorrelationId => _currentCorrelationId.Value!;
        public static DateTimeOffset CurrentStartTime => _currentStartTime.Value;

        #region Constructores
        public OperationLogScope(Logger logger, string source, string operation, string correlationId, string userId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source;
            _operation = operation;
            _correlationId = correlationId ?? Guid.NewGuid().ToString(); //si viene null, generaro uno
            _userId = userId;
            _start = DateTimeOffset.UtcNow;

            // Guardar correlationId y start time en AsyncLocal
            _currentCorrelationId.Value = _correlationId;
            _currentStartTime.Value = _start;

            //Fire-and-forget; aquí hago fire-and-forget para no bloquear registrando el logg.
            Task _ = _logger.AddAsync(new LogEntry
            {
                Level = "Information",
                Source = _source,
                Operation = _operation,
                Message = "Entering operation",
                CorrelationId = _correlationId,
                UserId = _userId
            });
        }
        #endregion

        #region Métodos públicos
        /// <summary>
        /// Dispose asíncrono registra salida con duración cuando se cierra el await using que lanzo la clase OperationLogScope
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            //Variables y objetos
            DateTimeOffset end = DateTimeOffset.UtcNow;
            long durationMs = Convert.ToInt64((end - _start).TotalMilliseconds);

            //Creo el log de salida
            LogEntry exitEntry = new LogEntry
            {
                Level = "Information",
                Source = _source,
                Operation = _operation,
                Message = "Exiting operation",
                CorrelationId = _correlationId,
                UserId = _userId,
                DurationMs = durationMs
            };

            //Guarda en DB el log
            await _logger.AddAsync(exitEntry);
        }
        #endregion
    }
}
