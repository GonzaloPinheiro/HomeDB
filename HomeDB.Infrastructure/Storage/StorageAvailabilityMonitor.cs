using System.Collections.Concurrent;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Observability;
using LogLevel = HomeDB.Domain.Common.Enums.LogLevel;

namespace HomeDB.Infrastructure.Storage
{
    /// <summary>
    /// Lógica del vigilante de discos, separada del BackgroundService para poder probarla sin temporizadores.
    /// En cada comprobación detecta los cambios de estado de cada disco y registra:
    /// - un aviso Critical cuando un disco pasa de utilizable a no utilizable;
    /// - un recordatorio Critical como máximo una vez cada 24 h mientras siga caído (no uno por comprobación);
    /// - un aviso Information cuando se recupera.
    /// Un disco es utilizable si está disponible y se puede escribir en él.
    /// </summary>
    public class StorageAvailabilityMonitor
    {
        //Tiempo mínimo entre dos avisos Critical del mismo disco mientras siga caído
        public static readonly TimeSpan ReminderInterval = TimeSpan.FromDays(1);

        private const string LogSource = "HomeDB.Infrastructure.Storage.StorageAvailabilityMonitor";

        //Variables y objetos
        private readonly IStorageAvailabilityService _availabilityService;
        private readonly Logger _logger;
        private readonly ConcurrentDictionary<string, DiskMonitorState> _states = new(StringComparer.OrdinalIgnoreCase);

        //Constructores
        public StorageAvailabilityMonitor(IStorageAvailabilityService availabilityService, Logger logger)
        {
            _availabilityService = availabilityService;
            _logger = logger;
        }

        /// <summary>
        /// Toma como estado inicial el resultado de la comprobación de arranque, para no volver a avisar de un
        /// disco que el validador de arranque ya reportó como caído.
        /// </summary>
        public void SeedFromCachedStatuses(DateTime nowUtc)
        {
            foreach (DiskStatus status in _availabilityService.GetCachedStatuses())
                _states[status.DiskKey] = new DiskMonitorState(IsUsable(status), nowUtc);
        }

        /// <summary>
        /// Comprueba todos los discos y registra los cambios de estado.
        /// </summary>
        public async Task CheckAsync(DateTime nowUtc)
        {
            IReadOnlyList<DiskStatus> statuses = _availabilityService.CheckAllDisks();

            foreach (DiskStatus status in statuses)
            {
                bool usable = IsUsable(status);
                _states.TryGetValue(status.DiskKey, out DiskMonitorState? previous);

                if (!usable)
                {
                    //Primera observación o recién caído: aviso inmediato
                    if (previous == null || previous.Usable)
                    {
                        await LogAsync(LogLevel.Critical, $"El disco '{status.DiskKey}' ha dejado de estar disponible ({status.Reason}). " +
                                                          "Las operaciones que lo necesiten devolverán 503 hasta que se recupere.");
                        _states[status.DiskKey] = new DiskMonitorState(false, nowUtc);
                    }
                    //Sigue caído: recordatorio solo si ha pasado el intervalo desde el último aviso
                    else if (nowUtc - previous.LastCriticalAtUtc >= ReminderInterval)
                    {
                        await LogAsync(LogLevel.Critical, $"Recordatorio: el disco '{status.DiskKey}' sigue sin estar disponible ({status.Reason}).");
                        _states[status.DiskKey] = previous with { LastCriticalAtUtc = nowUtc };
                    }
                }
                else
                {
                    if (previous != null && !previous.Usable)
                        await LogAsync(LogLevel.Information, $"El disco '{status.DiskKey}' vuelve a estar disponible.");

                    _states[status.DiskKey] = new DiskMonitorState(true, nowUtc);
                }
            }
        }

        private static bool IsUsable(DiskStatus status) => status.IsAvailable && status.IsWritable;

        private Task LogAsync(LogLevel level, string message)
        {
            return _logger.AddAsync(new LogEntry
            {
                Level = level.ToString(),
                Source = LogSource,
                Operation = nameof(CheckAsync),
                Message = message
            });
        }

        //Estado por disco: si estaba utilizable en la última comprobación y cuándo se avisó por última vez
        private sealed record DiskMonitorState(bool Usable, DateTime LastCriticalAtUtc);
    }
}
