using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Storage
{
    public class StorageLocationResolver : IStorageLocationResolver
    {
        private readonly StorageOptions _storageOptions;

        public StorageLocationResolver(IOptions<StorageOptions> options)
        {
            _storageOptions = options.Value;
        }

        /// <summary>
        /// Devuelve la ruta por defecto del tipo de uso indicado
        /// </summary>
        public string ResolveDefaultBasePath(LocationUsage usage)
        {
            (DiskOptions disk, StorageLocationOptions location) = FindDefaultLocation(usage);
            return Path.Combine(disk.Path, location.SubPath);
        }

        public string ResolveDefaultLocationKey(LocationUsage usage)
        {
            (_, StorageLocationOptions location) = FindDefaultLocation(usage);
            return location.Key;
        }

        public IReadOnlyCollection<string> GetAllLocationKeys()
        {
            return _storageOptions.Disks
                .SelectMany(disk => disk.Locations)
                .Select(location => location.Key)
                .ToList();
        }

        private (DiskOptions Disk, StorageLocationOptions Location) FindDefaultLocation(LocationUsage usage)
        {
            foreach (DiskOptions disk in _storageOptions.Disks)
            {
                foreach (StorageLocationOptions location in disk.Locations)
                {
                    if (location.Usage == usage && location.IsDefault)
                        return (disk, location);
                }
            }

            //No debería ocurrir: StorageOptions.Validate() exige una ubicación por defecto para cada Usage
            //presente en la config. Si llega aquí, es que algo pidió un Usage que ni siquiera está configurado.
            throw new InvalidOperationException($"No hay ninguna ubicación por defecto configurada para el uso '{usage}'.");
        }
    }
}
