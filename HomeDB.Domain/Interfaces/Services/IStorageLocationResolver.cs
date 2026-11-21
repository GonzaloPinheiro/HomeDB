using HomeDB.Domain.Common;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IStorageLocationResolver
    {
        /// <summary>
        /// Devuelve la ruta absoluta de la ubicación marcada como por defecto para el uso indicado.
        /// </summary>
        string ResolveDefaultBasePath(LocationUsage usage);

        /// <summary>
        /// Devuelve la clave de la ubicación marcada como por defecto para el uso indicado.
        /// </summary>
        string ResolveDefaultLocationKey(LocationUsage usage);

        /// <summary>
        /// Devuelve la clave del disco al que pertenece la ubicación marcada como por defecto para el uso indicado.
        /// </summary>
        string ResolveDefaultDiskKey(LocationUsage usage);

        /// <summary>
        /// Devuelve la ruta absoluta de la ubicación con la clave indicada.
        /// </summary>
        string ResolveBasePath(string locationKey);

        /// <summary>
        /// Devuelve la clave del disco al que pertenece la ubicación con la clave indicada.
        /// </summary>
        string ResolveDiskKey(string locationKey);

        /// <summary>
        /// Devuelve todas las claves de ubicación declaradas en la configuración, de todos los discos.
        /// </summary>
        IReadOnlyCollection<string> GetAllLocationKeys();
    }
}
