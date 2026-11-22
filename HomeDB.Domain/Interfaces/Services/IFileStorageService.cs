using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Services
{
    /// <summary>
    /// Acceso a los ficheros físicos. Todas las operaciones comprueban antes que el disco que respalda la ubicación
    /// está disponible, y lanzan StorageLocationUnavailableException si no lo está.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un stream en la ubicación personal por defecto con el nombre indicado.
        /// </summary>
        Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Guarda un stream en el área temporal, en la ruta absoluta indicada (que debe estar dentro de ella).
        /// Crea la carpeta que contiene el fichero si no existe.
        /// </summary>
        Task SaveTempAsync(Stream stream, string tempFilePath, CancellationToken cancellationToken);

        /// <summary>
        /// Mueve un archivo ya existente en disco (p. ej. un temporal ensamblado) a su ubicación final, evitando
        /// una segunda copia completa del contenido cuando origen y destino están en el mismo volumen.
        /// </summary>
        Task SaveFromPathAsync(string sourceFilePath, string locationKey, string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Elimina el fichero físico del disco si existe.
        /// </summary>
        Task DeleteAsync(string locationKey, string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Comprueba si el fichero existe en disco.
        /// </summary>
        bool Exists(string locationKey, string storedName);

        /// <summary>
        /// Devuelve la ruta completa del archivo en disco.
        /// </summary>
        string GetFilePath(string locationKey, string storedName);
    }
}
