using HomeDB.Domain.Entities;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Infrastructure.Observability
{
    public class LogBackgroundService : BackgroundService, ILogQueue
    {
        private readonly Channel<LogEntry> _channel;
        private readonly ILogEntryRepository _repository;
        private readonly LogFailureFileSink _failureSink;

        /// <summary>
        /// Crea la cola y recibe el repositorio para persistencia.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="failureSink"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LogBackgroundService(ILogEntryRepository repository, LogFailureFileSink failureSink)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _failureSink = failureSink ?? throw new ArgumentNullException(nameof(failureSink));

            BoundedChannelOptions options = new BoundedChannelOptions(10000) //Cantidad máxima de la cola de logs
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest //Descarta logs antiguos si está lleno
            };

            _channel = Channel.CreateBounded<LogEntry>(options);
        }

        /// <summary>
        /// Agrega un log a la cola sin bloquear la petición.
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
            entry.TimeStamp = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(entry.CorrelationId))
            {
                entry.CorrelationId = OperationLogScope.CurrentCorrelationId ?? Guid.NewGuid().ToString();
            }

            // TryWrite no bloquea; si falla, la cola está llena y descartamos el log.
            bool escrito = _channel.Writer.TryWrite(entry);
            if (!escrito)
            {
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
                            //Fallo el insert, se guarda el fallo en archivo dentro de la carpeta de logs de la api.
                            //WriteAsync ya es su propia red de seguridad: si ni el fichero funciona, cae a consola.
                            await _failureSink.WriteAsync(LogFailureType.InsertFailure, item, ex, stoppingToken).ConfigureAwait(false);
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
                        //Fallo el insert durante el flush final; mismo fallback a fichero que en ExecuteAsync.
                        await _failureSink.WriteAsync(LogFailureType.InsertFailure, item, ex, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
