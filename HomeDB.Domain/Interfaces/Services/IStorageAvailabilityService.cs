using HomeDB.Domain.Common.RecordsInfrastructure;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IStorageAvailabilityService
    {
        /// <summary>
        /// Comprobación barata, pensada para ejecutarse en cada operación: verifica en vivo el fichero marcador del
        /// disco. Si forWrite es true, además rechaza el disco cuando la última comprobación completa detectó que
        /// no se podía escribir en él.
        /// </summary>
        /// <exception cref="HomeDB.Domain.Exceptions.StorageLocationUnavailableException">El disco no está disponible.</exception>
        void EnsureAvailable(string diskKey, bool forWrite);

        /// <summary>
        /// Comprobación completa de un disco (marcador y fichero sonda de escritura). Actualiza el estado guardado.
        /// </summary>
        DiskStatus CheckDisk(string diskKey);

        /// <summary>
        /// Comprobación completa de todos los discos declarados en la configuración. Actualiza el estado guardado.
        /// </summary>
        IReadOnlyList<DiskStatus> CheckAllDisks();

        /// <summary>
        /// Capacidad del sistema de ficheros del disco, leída en el momento. Devuelve null si el disco no está
        /// confirmado por su marcador: si no está montado, su carpeta es un punto de montaje vacío y su capacidad
        /// sería la del disco que lo contiene, no la del disco ausente.
        /// </summary>
        DiskCapacity? GetCapacity(string diskKey);

        /// <summary>
        /// Último estado guardado de un disco, o null si todavía no se ha hecho ninguna comprobación completa.
        /// </summary>
        DiskStatus? GetCachedStatus(string diskKey);

        /// <summary>
        /// Último estado guardado de todos los discos que ya se han comprobado.
        /// </summary>
        IReadOnlyList<DiskStatus> GetCachedStatuses();
    }
}
