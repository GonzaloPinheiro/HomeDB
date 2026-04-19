using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;

namespace HomeDB.Infrastructure.Observability
{
    public class Logger
    {
        //Variables y ojetos
        private readonly ILogQueue _logQueue;
        private readonly ILogEntryRepository _logEntryRepository;

        #region Constructores
        public Logger(ILogEntryRepository logEntryRepository, ILogQueue logQueue)
        {
            _logEntryRepository = logEntryRepository ?? throw new ArgumentNullException(nameof(logEntryRepository));
            _logQueue = logQueue ?? throw new ArgumentNullException(nameof(logQueue));
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
                Console.WriteLine("En AddAsync(log_entry entry) se ha obtenido un log con estado null");
                return;
            }

            // Delegar al ILogQueue (rápido: encola y devuelve)
            await _logQueue.EnqueueAsync(entry).ConfigureAwait(false);
        }

        /// <summary>
        /// Comienza un scope de operación que registra entrada y, al disponer, registra salida con duración.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="operation"></param>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public OperationLogScope BeginScope(string source, string operation, string correlationId = null, string userId = null)
        {
            OperationLogScope scope = new OperationLogScope(this, source, operation, correlationId, userId);
            return scope;
        }
        #endregion
    }
}
