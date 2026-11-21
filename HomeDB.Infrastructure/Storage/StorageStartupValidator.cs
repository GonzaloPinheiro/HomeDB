using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.Options;
using LogLevel = HomeDB.Domain.Common.Enums.LogLevel;

namespace HomeDB.Infrastructure.Storage
{
    /// <summary>
    /// Aplica al arrancar la política de arranque de cada disco declarado en Storage:Disks:
    /// - Required: si no está disponible y escribible, la API no arranca.
    /// - Optional: la API arranca, el disco queda no disponible y se registra un aviso Critical.
    /// </summary>
    public class StorageStartupValidator
    {
        //Variables y objetos
        private readonly IStorageAvailabilityService _availabilityService;
        private readonly StorageOptions _storageOptions;
        private readonly Logger _logger;

        //Constructores
        public StorageStartupValidator(IStorageAvailabilityService availabilityService, IOptions<StorageOptions> options, Logger logger)
        {
            _availabilityService = availabilityService;
            _storageOptions = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Comprueba todos los discos (marcador y escritura) y aplica su política de arranque.
        /// </summary>
        /// <exception cref="InvalidOperationException">Algún disco Required no está disponible o no es escribible.</exception>
        public async Task ValidateAsync()
        {
            IReadOnlyList<DiskStatus> statuses = _availabilityService.CheckAllDisks();
            List<string> requiredFailures = new List<string>();

            foreach (DiskOptions disk in _storageOptions.Disks)
            {
                DiskStatus status = statuses.First(s => string.Equals(s.DiskKey, disk.Key, StringComparison.OrdinalIgnoreCase));

                //Un disco solo es utilizable si está disponible y se puede escribir en él
                if (status.IsAvailable && status.IsWritable)
                    continue;

                string reason = status.Reason ?? "unknown reason";

                if (disk.StartupPolicy == DiskStartupPolicy.Required)
                {
                    //La ruta solo aparece aquí: este mensaje va a la consola del arranque, nunca a un cliente
                    requiredFailures.Add($"'{disk.Key}' ({disk.Path}): {reason}");
                }
                else
                {
                    await _logger.AddAsync(new LogEntry
                    {
                        Level = LogLevel.Critical.ToString(),
                        Source = "HomeDB.Infrastructure.Storage.StorageStartupValidator",
                        Operation = nameof(ValidateAsync),
                        Message = $"El disco opcional '{disk.Key}' no está disponible al arrancar ({reason}). " +
                                  "Las operaciones que lo necesiten devolverán 503 hasta que vuelva a estar disponible."
                    });
                }
            }

            if (requiredFailures.Count > 0)
                throw new InvalidOperationException(
                    "No se puede arrancar: hay discos obligatorios (StartupPolicy=Required) no disponibles: " +
                    string.Join("; ", requiredFailures) + ".");
        }
    }
}
