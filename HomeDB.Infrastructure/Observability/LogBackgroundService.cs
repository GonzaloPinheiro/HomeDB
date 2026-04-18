using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Repositories;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace HomeDB.Infrastructure.Observability
{
    public class LogBackgroundService : BackgroundService, ILogQueue
    {
        private readonly Channel<LogEntry> _channel;
        private readonly LogEntryRepository _repository;

        /// <summary>
        /// Crea la cola y recibe el repositorio para persistencia.
        /// </summary>
        /// <param name="repository"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LogBackgroundService(LogEntryRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            BoundedChannelOptions options = new BoundedChannelOptions(10000) //Cantidad máxima de la cola de logs
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest //Descarta logs antiguos si está lleno
            };

            _channel = Channel.CreateBounded<LogEntry>(options);
        }

        /// <summary>
        /// Agrega un log ala cola sin bloquear la petición.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public Task EnqueueAsync(LogEntry entry)
        {
            if (entry == null)
            {
                Console.WriteLine("EnqueueAsync: entry null");
                return Task.CompletedTask;
            }

            // Rellenar campos derivados aquí, fuera del POCO
            entry.timeStamp = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(entry.correlationId))
            {
                entry.correlationId = OperationLogScope.CurrentCorrelationId ?? Guid.NewGuid().ToString();
            }

            // TryWrite no bloquea; si falla, la cola está llena y descartamos el log.
            bool escrito = _channel.Writer.TryWrite(entry);
            if (!escrito)
            {
                // Aquí puedes registrar métrica o traza de que se descartó un log
                Console.WriteLine("La cola de logs está llena. Se descartó un log.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Worker que consume la cola y persiste los logs en la base de datos.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out LogEntry item))
                    {
                        try
                        {
                            // Aquí sí hacemos await a la BD, pero en el worker (no en la petición)
                            await _repository.InsertLogAsync(item, stoppingToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // Manejo simple: escribir en consola y continuar.
                            Console.WriteLine("Error guardando log: " + ex);
                            // En producción: retry, DLQ, métricas, etc.
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada al parar la app
            }
        }

        /// <summary>
        /// Al parar el host, indicamos que no habrá más writes y hacemos flush de lo restante.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Writer.Complete();

            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out LogEntry item))
                {
                    try
                    {
                        await _repository.InsertLogAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error flush final de logs: " + ex);
                    }
                }
            }

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
