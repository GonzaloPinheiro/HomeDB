using System.Collections.Concurrent;
using HomeDB.Application.Options;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Storage
{
    /// <summary>
    /// Comprueba que cada disco declarado en Storage:Disks esté realmente disponible, no solo que su ruta exista.
    /// Si un disco no está montado en el host, Docker deja una carpeta vacía en su lugar y desde dentro del contenedor
    /// no se distingue de un disco recién formateado. El fichero marcador, que se crea una vez en la raíz de cada
    /// disco real con la clave del disco como contenido, es lo que permite distinguirlos.
    /// Los motivos de fallo son textos genéricos: pueden llegar al cliente en la respuesta 503.
    /// </summary>
    public class StorageAvailabilityService : IStorageAvailabilityService
    {
        public const string MarkerFileName = ".homedb-location";
        private const string ProbeFilePrefix = ".homedb-probe-";

        //Variables y objetos
        private readonly StorageOptions _storageOptions;
        private readonly ConcurrentDictionary<string, DiskStatus> _cache = new(StringComparer.OrdinalIgnoreCase);

        //Constructores
        public StorageAvailabilityService(IOptions<StorageOptions> options)
        {
            _storageOptions = options.Value;
        }

        public void EnsureAvailable(string diskKey, bool forWrite)
        {
            DiskOptions disk = FindDisk(diskKey);

            //El marcador se comprueba siempre en vivo: leer un fichero pequeño es barato y detecta al instante
            //un disco que se ha desmontado.
            string? markerFailure = CheckMarker(disk);
            if (markerFailure != null)
                throw new StorageLocationUnavailableException(disk.Key, markerFailure);

            //La sonda de escritura es más cara, así que solo se usa su último resultado guardado. Se ignora si el
            //fallo guardado fue de marcador (IsAvailable false): ya se ha comprobado en vivo y el disco puede
            //haberse recuperado desde entonces.
            if (forWrite && _cache.TryGetValue(disk.Key, out DiskStatus? cached) && cached.IsAvailable && !cached.IsWritable)
                throw new StorageLocationUnavailableException(disk.Key, cached.Reason ?? "disk is not writable");
        }

        public DiskStatus CheckDisk(string diskKey)
        {
            DiskOptions disk = FindDisk(diskKey);

            string? failure = CheckMarker(disk);
            bool isAvailable = failure == null;
            bool isWritable = false;

            //Sin marcador válido no se prueba la escritura: no se debe escribir nada en un disco no confirmado.
            if (isAvailable)
            {
                failure = ProbeWrite(disk);
                isWritable = failure == null;
            }

            DiskStatus status = new DiskStatus(disk.Key, isAvailable, isWritable, failure, DateTime.UtcNow);
            _cache[disk.Key] = status;
            return status;
        }

        public IReadOnlyList<DiskStatus> CheckAllDisks()
        {
            return _storageOptions.Disks
                .Select(disk => CheckDisk(disk.Key))
                .ToList();
        }

        public DiskStatus? GetCachedStatus(string diskKey)
        {
            return _cache.TryGetValue(diskKey, out DiskStatus? status) ? status : null;
        }

        public IReadOnlyList<DiskStatus> GetCachedStatuses()
        {
            return _cache.Values.ToList();
        }

        #region Helpers
        private DiskOptions FindDisk(string diskKey)
        {
            DiskOptions? disk = _storageOptions.Disks
                .FirstOrDefault(d => string.Equals(d.Key, diskKey, StringComparison.OrdinalIgnoreCase));

            //Una clave de disco desconocida es un error de programación, no un fallo de disponibilidad.
            if (disk == null)
                throw new InvalidOperationException($"El disco '{diskKey}' no está declarado en Storage:Disks.");

            return disk;
        }

        /// <summary>
        /// Devuelve el motivo del fallo, o null si el marcador existe y su contenido es la clave del disco.
        /// </summary>
        private static string? CheckMarker(DiskOptions disk)
        {
            try
            {
                if (!Directory.Exists(disk.Path))
                    return "disk path is not accessible";

                string markerPath = Path.Combine(disk.Path, MarkerFileName);
                if (!File.Exists(markerPath))
                    return "marker file is missing";

                string content = File.ReadAllText(markerPath).Trim();
                if (!string.Equals(content, disk.Key, StringComparison.OrdinalIgnoreCase))
                    return "marker file does not match the disk";

                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return "marker file could not be read";
            }
        }

        /// <summary>
        /// Crea y borra un fichero sonda en la raíz del disco. Los docks USB suelen remontarse en solo lectura
        /// ante errores, y eso solo se detecta intentando escribir. Devuelve el motivo del fallo, o null.
        /// </summary>
        private static string? ProbeWrite(DiskOptions disk)
        {
            string probePath = Path.Combine(disk.Path, $"{ProbeFilePrefix}{Guid.NewGuid():N}");

            try
            {
                File.WriteAllBytes(probePath, new byte[] { 0 });
                File.Delete(probePath);
                return null;
            }
            catch (Exception ex) 
            when (ex is IOException or UnauthorizedAccessException)
            {
                //Intentar no dejar el fichero sonda si se llegó a crear
                try { File.Delete(probePath); } catch (Exception) { }
                return "disk is not writable";
            }
        }
        #endregion
    }
}
