using HomeDB.Application.Options;
using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using LogLevel = HomeDB.Domain.Common.Enums.LogLevel;

namespace HomeDB.Infrastructure.Storage
{
    //Background service que comprueba periódicamente la disponibilidad de los discos (marcador y escritura),
    //mantiene actualizado el estado guardado y avisa de las caídas y recuperaciones.
    public class StorageAvailabilityBackgroundService : BackgroundService
    {
        //Variables y objetos globales
        private readonly StorageAvailabilityMonitor _monitor;
        private readonly IOptions<StorageOptions> _options;
        private readonly Logger _logger;

        //Constructores
        public StorageAvailabilityBackgroundService(StorageAvailabilityMonitor monitor, IOptions<StorageOptions> options, Logger logger)
        {
            _monitor = monitor;
            _options = options;
            _logger = logger;
        }

        //Se ejecuta al inicio de forma indefinida hasta que se cancele con cToken
        protected override async Task ExecuteAsync(CancellationToken cToken)
        {
            //El validador de arranque ya comprobó los discos: partir de ese resultado evita avisar dos veces
            _monitor.SeedFromCachedStatuses(DateTime.UtcNow);

            //PeriodicTimer dispara un "tick" cada intervalo configurado
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Value.AvailabilityCheckIntervalSeconds));

            try
            {
                //Esperar a que se active el tick de forma indefinida hasta que cToken lo detenga
                while (await timer.WaitForNextTickAsync(cToken))
                {
                    await RunCheckSafelyAsync();
                }
            }
            catch (OperationCanceledException)
            {
                //Cancelación esperada al parar la app
            }
        }

        //Una comprobación que falle no debe matar el servicio: se registra y se reintenta en el siguiente tick
        private async Task RunCheckSafelyAsync()
        {
            try
            {
                await _monitor.CheckAsync(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await _logger.AddAsync(new LogEntry
                {
                    Level = LogLevel.Error.ToString(),
                    Source = "HomeDB.Infrastructure.Storage.StorageAvailabilityBackgroundService",
                    Operation = nameof(RunCheckSafelyAsync),
                    Message = "Error comprobando la disponibilidad de los discos",
                    Exception = ex.ToString()
                });
            }
        }
    }
}
