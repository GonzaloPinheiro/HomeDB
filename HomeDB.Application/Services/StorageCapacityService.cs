using HomeDB.Application.Options;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    /// <summary>
    /// Comprueba si algo cabe en una ubicación respetando sus límites de espacio. Lo aplican quienes escriben:
    /// las subidas hoy; torrents y conversión de vídeo después.
    /// </summary>
    public class StorageCapacityService
    {
        //Variables y objetos globales
        private readonly StorageOptions _storageOptions;
        private readonly IStorageAvailabilityService _availabilityService;
        private readonly IFileItemRepository _fileItemRepository;

        //Constructores
        public StorageCapacityService(IOptions<StorageOptions> storageOptions, IStorageAvailabilityService availabilityService,
                                      IFileItemRepository fileItemRepository)
        {
            _storageOptions = storageOptions.Value;
            _availabilityService = availabilityService;
            _fileItemRepository = fileItemRepository;
        }

        /// <summary>
        /// Comprueba que guardar la cantidad de bytes indicada en la ubicación no rebasa sus límites:
        /// - MinFreeBytes del disco: tras guardar, el espacio libre no puede quedar por debajo de ese mínimo.
        /// - MaxUsedBytes de la ubicación: lo que HomeDB ya tiene registrado en ella más lo nuevo no puede superar el tope.
        /// </summary>
        /// <exception cref="StorageLocationFullException">No cabe por alguno de los dos límites.</exception>
        /// <exception cref="StorageLocationUnavailableException">No se pudo leer la capacidad del disco.</exception>
        public async Task EnsureFitsAsync(string locationKey, long bytes, CancellationToken cToken)
        {
            (DiskOptions disk, StorageLocationOptions location) = FindLocation(locationKey);

            //Si no se puede leer la capacidad se rechaza (falla cerrado): MinFreeBytes existe para proteger el disco
            //donde vive PostgreSQL, y es peor dejar pasar una escritura que rechazar una por un error puntual.
            DiskCapacity? capacity = _availabilityService.GetCapacity(disk.Key);
            if (capacity == null)
                throw new StorageLocationUnavailableException(disk.Key, "capacity could not be read");

            //El espacio libre mínimo no se debe rebasar: quedar justo en el mínimo es válido
            if (capacity.FreeBytes - bytes < disk.MinFreeBytes)
                throw new StorageLocationFullException(location.Key, "not enough free space");

            //El tope de uso exige contar lo que HomeDB ha puesto en la ubicación; se consulta solo si hay tope
            if (location.MaxUsedBytes.HasValue)
            {
                long usedBytes = await _fileItemRepository.GetUsedBytesByLocationAsync(location.Key, cToken);
                if (usedBytes + bytes > location.MaxUsedBytes.Value)
                    throw new StorageLocationFullException(location.Key, "usage limit reached");
            }
        }

        private (DiskOptions Disk, StorageLocationOptions Location) FindLocation(string locationKey)
        {
            foreach (DiskOptions disk in _storageOptions.Disks)
            {
                foreach (StorageLocationOptions location in disk.Locations)
                {
                    if (string.Equals(location.Key, locationKey, StringComparison.OrdinalIgnoreCase))
                        return (disk, location);
                }
            }

            //No debería ocurrir: las claves llegan del resolver, que sale de la misma configuración
            throw new InvalidOperationException($"La ubicación '{locationKey}' no está declarada en Storage:Disks.");
        }
    }
}
