using HomeDB.Application.Options;
using HomeDB.Application.Services;
using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Storage
{
    //Background service que se ejecuta de forma periódica para limpiar chunks de upload incompletos y caducados.
    public class UploadCleanupBackgroundService : BackgroundService
    {
        //Variables y objetos globales
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<UploadCleanupOptions> _options;
        private readonly Logger _logger;

        //Constructores
        public UploadCleanupBackgroundService(IServiceScopeFactory scopeFactory, IOptions<UploadCleanupOptions> options, 
                                              Logger logger) 
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        //Se ejecuta al inicio de forma indefinida hasta que se cancele con cToken
        protected override async Task ExecuteAsync(CancellationToken cToken)
        {
            //Calcular el tiempo hasta la próxima ejecución programada al lanzar la api
            TimeSpan initialDelay = CalculateDelayUntilNextRun(_options.Value.RunAtHourUtc);

            //Esperar hasta la próxima hota de ejecución programada
            await Task.Delay(initialDelay, cToken);

            //PeriodicTimer dispara un "tick" cada 24 horas.
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromDays(1));

            //Esperar a que se active el tick de forma indefinida hasta que stoppingToken lo detenga
            do
            {
                await RunCleanupSafelyAsync(cToken);
            }
            while (await timer.WaitForNextTickAsync(cToken));
        }

        // Ejecuta la limpieza de sesiones de upload finalizadas.
        private async Task RunCleanupSafelyAsync(CancellationToken cToken)
        {
            //Crear un nuevo scope para resolver los servicios necesarios
            using IServiceScope scope = _scopeFactory.CreateScope();

            //Obtener el servicio UploadService desde el scope
            UploadService sessionService = scope.ServiceProvider.GetRequiredService<UploadService>();

            //Lanzar la limpieza de sesiones del uploadService
            try
            {
                await sessionService.CleanupFinishedSessionsAsync(cToken);
            }
            catch (Exception ex)
            {
                await _logger.AddAsync(new LogEntry
                {
                    Level = "Error",
                    Source = "HomeDB.Infrastructure.Storage.UploadCleanupBackgroundService",
                    Operation = nameof(RunCleanupSafelyAsync),
                    Message = "Error en cleanup",
                    Exception = ex.ToString()
                });
            }
        }

        //Calcula el tiempo restante hasta la próxima ejecución programada a la hora especificada en UTC.
        private static TimeSpan CalculateDelayUntilNextRun(int runAtHourUtc)
        {
            //Obtener la hora actual en UTC y calcular la próxima ejecución programada
            DateTime nowUtc = DateTime.UtcNow;
            DateTime nextRun = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, runAtHourUtc, 0, 0, DateTimeKind.Utc);

            //Si la hora de ejecución ya pasó hoy, programar para mañana
            if (nextRun <= nowUtc)
                nextRun = nextRun.AddDays(1);

            //Calcular el tiempo restante hasta la próxima ejecución
            return nextRun - nowUtc;
        }
    }
}