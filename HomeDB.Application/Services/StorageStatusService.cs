using HomeDB.Application.DTOs;
using HomeDB.Application.Options;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class StorageStatusService
    {
        //Variables y objetos globales
        private readonly StorageOptions _storageOptions;
        private readonly IStorageAvailabilityService _availabilityService;

        //Constructores
        public StorageStatusService(IOptions<StorageOptions> storageOptions, IStorageAvailabilityService availabilityService)
        {
            _storageOptions = storageOptions.Value;
            _availabilityService = availabilityService;
        }

        /// <summary>
        /// Devuelve el estado de cada ubicación declarada en la configuración, en el orden en que aparecen.
        /// La disponibilidad es la de la última comprobación completa (como mucho Storage:AvailabilityCheckIntervalSeconds
        /// de antigüedad); la capacidad se lee en el momento y solo si el disco está disponible.
        /// </summary>
        public IReadOnlyList<StorageLocationStatusDto> GetLocationsStatus()
        {
            List<StorageLocationStatusDto> result = new List<StorageLocationStatusDto>();

            foreach (DiskOptions disk in _storageOptions.Disks)
            {
                //Si todavía no hay ninguna comprobación guardada, se hace una ahora
                DiskStatus status = _availabilityService.GetCachedStatus(disk.Key) ?? _availabilityService.CheckDisk(disk.Key);
                DiskCapacity? capacity = status.IsAvailable ? _availabilityService.GetCapacity(disk.Key) : null;

                //Se devuelve una entrada por cada ubicación del disco, aunque el disco no esté disponible.
                foreach (StorageLocationOptions location in disk.Locations)
                {
                    result.Add(new StorageLocationStatusDto(
                        location.Key, disk.Key, location.Usage, location.IsDefault,
                        status.IsAvailable, status.IsWritable, status.Reason, status.CheckedAtUtc,
                        capacity?.TotalBytes, capacity?.UsedBytes, capacity?.FreeBytes,
                        disk.MinFreeBytes, location.MaxUsedBytes));
                }
            }

            return result;
        }
    }
}
