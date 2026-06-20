using HomeDB.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.SystemMonitoring
{
    public class SystemMetricsBackgroundService : BackgroundService
    {
        //Variables y objetos globales
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _sampleInterval;
        private readonly TimeSpan _retentionPeriod;

        //Constructores
        public SystemMetricsBackgroundService(IServiceScopeFactory scopeFactory, IOptions<SystemMetricsOptions> options)
        {
            _scopeFactory = scopeFactory;
            _sampleInterval = TimeSpan.FromMinutes(options.Value.SampleIntervalMinutes);
            _retentionPeriod = TimeSpan.FromDays(options.Value.RetentionDays);
        }

        //Se ejecuta al inicio de forma indefinida hasta que se cancele el stoppingToken
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Primera captura inmediata al arrancar la app.
            await CaptureMetricsSafelyAsync(stoppingToken);
            await PurgeOldEntriesSafelyAsync(stoppingToken);

            //PeriodicTimer dispara un "tick" cada _sampleInterval.
            using PeriodicTimer timer = new PeriodicTimer(_sampleInterval);

            //Esperar a que se active el tick de forma indefinida hasta que stoppingToken lo detenga 
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                //Captura y limpia las métricas de la base de datos
                await CaptureMetricsSafelyAsync(stoppingToken);
                await PurgeOldEntriesSafelyAsync(stoppingToken);
            }
        }

        //Captura las métricas actuales del sistema y las guarda en la base de datos
        private async Task CaptureMetricsSafelyAsync(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                SystemMetricsService service = scope.ServiceProvider.GetRequiredService<SystemMetricsService>();
                await service.CaptureAndStoreAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMetricsBackgroundService] Error al capturar métricas: {ex.Message}");
            }
        }

        //Elimina registros mas antiguos de los configurados en appsettings
        private async Task PurgeOldEntriesSafelyAsync(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                SystemMetricsService service = scope.ServiceProvider.GetRequiredService<SystemMetricsService>();
                await service.PurgeOldEntriesAsync(_retentionPeriod, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMetricsBackgroundService] Error al purgar histórico: {ex.Message}");
            }
        }
    }
}