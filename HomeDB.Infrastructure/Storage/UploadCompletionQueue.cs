using System.Threading.Channels;
using HomeDB.Application.Services;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeDB.Infrastructure.Storage
{
    //Worker en background que ensambla y finaliza las subidas encoladas, para no bloquear la request HTTP de /complete.
    public class UploadCompletionQueue : BackgroundService, IUploadCompletionQueue
    {
        //Variables y objetos globales
        private readonly Channel<(Guid SessionId, int OwnerId)> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Logger _logger;

        //Constructores
        public UploadCompletionQueue(IServiceScopeFactory scopeFactory, Logger logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            BoundedChannelOptions options = new BoundedChannelOptions(100) //Subidas simultáneas en espera de ser ensambladas
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait //A diferencia de los logs, no se puede descartar una finalización de subida
            };

            _channel = Channel.CreateBounded<(Guid SessionId, int OwnerId)>(options);
        }

        //Encola la finalización de la sesión indicada; no espera a que se procese.
        public async Task QueueCompletionAsync(Guid sessionId, int ownerId)
        {
            await _channel.Writer.WriteAsync((sessionId, ownerId)).ConfigureAwait(false);
        }

        //Procesa las finalizaciones encoladas de forma indefinida hasta que se cancele con cToken.
        protected override async Task ExecuteAsync(CancellationToken cToken)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(cToken).ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out (Guid SessionId, int OwnerId) item))
                    {
                        await ProcessItemSafelyAsync(item.SessionId, item.OwnerId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Cancelación esperada al parar la app.
            }
        }

        //Procesa un item de la cola en su propio scope de DI, sin dejar que una excepción mate el loop del worker.
        private async Task ProcessItemSafelyAsync(Guid sessionId, int ownerId)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            UploadService uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

            try
            {
                //CancellationToken.None: la finalización debe completarse aunque el servidor esté apagándose;
                //ProcessCompletionAsync persiste el resultado (éxito o error) antes de devolver.
                await uploadService.ProcessCompletionAsync(sessionId, ownerId, CancellationToken.None);
                //CompleteUploadAsync
            }
            catch (Exception ex)
            {
                await _logger.AddAsync(new LogEntry
                {
                    Level = LogLevel.Error.ToString(),
                    Source = "HomeDB.Infrastructure.Storage.UploadCompletionQueue",
                    Operation = nameof(ProcessItemSafelyAsync),
                    Message = $"Error al procesar la finalización de la sesión de subida {sessionId}",
                    Exception = ex.ToString()
                });
            }
        }
    }
}
