using HomeDB.Application.Options;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Backup
{
    //Background service que comprueba periódicamente si algún nivel de backup (Daily, Monthly, ...) está pendiente y lo lanza.
    public class BackupBackgroundService : BackgroundService
    {
        //Margen tras la hora configurada durante el cual todavía se considera válido lanzar el backup (para no saltarse el día si se pierde el tick exacto, p.ej. tras un reinicio).
        private const int CatchUpWindowHours = 2;

        //Variables y objetos globales
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<BackupOptions> _options;
        private readonly Logger _logger;

        //Constructores
        public BackupBackgroundService(IServiceScopeFactory scopeFactory, IOptions<BackupOptions> options, Logger logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        //Se ejecuta al inicio de forma indefinida hasta que se cancele con cToken
        protected override async Task ExecuteAsync(CancellationToken cToken)
        {
            TimeSpan checkInterval = TimeSpan.FromMinutes(_options.Value.CheckIntervalMinutes);

            //PeriodicTimer dispara un "tick" cada checkInterval.
            using PeriodicTimer timer = new PeriodicTimer(checkInterval);

            //Comprobar todos los niveles de backup configurados en cada tick hasta que cToken lo detenga
            do
            {
                await RunDailyBackupIfDueSafelyAsync(cToken);
                //futuro: await RunMonthlyBackupIfDueSafelyAsync(cToken);
            }
            while (await timer.WaitForNextTickAsync(cToken));
        }

        //Comprueba si el backup diario está pendiente (intervalo cumplido y hora alcanzada) y, si es así, lo ejecuta.
        private async Task RunDailyBackupIfDueSafelyAsync(CancellationToken cToken) //TODO: Revisar flujo para ver que no puedan perder copias debido a cahídas puntuales
        {
            BackupLevelOptions dailyOptions = _options.Value.Daily;

            //Crear un nuevo scope para resolver los servicios necesarios
            using IServiceScope scope = _scopeFactory.CreateScope();

            //Obtener los servicios necesarios desde el scope
            IBackupAuditRepository backupAuditRepository = scope.ServiceProvider.GetRequiredService<IBackupAuditRepository>();
            IBackupService backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

            try
            {
                //Comprobar si ha pasado el intervalo configurado desde el último backup exitoso
                DateTime? lastSuccessfulCompletedAt = await backupAuditRepository.GetLastSuccessfulCompletedAtAsync(BackupLevel.Daily, cToken);
                bool intervalElapsed = lastSuccessfulCompletedAt is null
                    || DateTime.UtcNow - lastSuccessfulCompletedAt.Value >= TimeSpan.FromHours(dailyOptions.IntervalHours);

                //Solo lanzarlo si además estamos dentro de la ventana horaria configurada para este nivel (hora exacta + margen de recuperación)
                int hoursSinceScheduled = ((DateTime.UtcNow.Hour - dailyOptions.RunAtHourUtc) + 24) % 24;
                bool withinWindow = hoursSinceScheduled < CatchUpWindowHours;
                bool isDue = intervalElapsed && withinWindow;

                if (isDue)
                {
                    //Lanzar el backup diario
                    await backupService.TriggerBackupAsync(BackupLevel.Daily, cToken);

                    //Log de exito
                    await _logger.AddAsync(new LogEntry
                    {
                        Level = LogLevel.Information.ToString(),
                        Source = "HomeDB.Infrastructure.Backup.BackupBackgroundService",
                        Operation = nameof(RunDailyBackupIfDueSafelyAsync),
                        Message = "Backup diario completado"
                    });
                }
            }
            catch (BackupAlreadyRunningException)
            {
                //No es un error: ya hay un backup en curso simplemente se omite
                await _logger.AddAsync(new LogEntry
                {
                    Level = LogLevel.Information.ToString(),
                    Source = "HomeDB.Infrastructure.Backup.BackupBackgroundService",
                    Operation = nameof(RunDailyBackupIfDueSafelyAsync),
                    Message = "Backup diario omitido: ya hay uno en curso"
                });
            }
            catch (Exception ex)
            {
                //Log de error
                await _logger.AddAsync(new LogEntry
                {
                    Level = LogLevel.Error.ToString(),
                    Source = "HomeDB.Infrastructure.Backup.BackupBackgroundService",
                    Operation = nameof(RunDailyBackupIfDueSafelyAsync),
                    Message = "Error en el backup diario",
                    Exception = ex.ToString()
                });
            }
        }
    }
}