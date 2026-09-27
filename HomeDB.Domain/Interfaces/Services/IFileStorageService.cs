using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un stream en disco con el nombre indicado.
        /// </summary>
        Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Elimina el fichero físico del disco si existe.
        /// </summary>
        Task DeleteAsync(string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Comprueba si el fichero existe en disco.
        /// </summary>
        bool Exists(string storedName);

        /// <summary>
        /// Devuelve la ruta completa del archivo en disco.
        /// </summary>
        string GetFilePath(string storedName);
    }
}