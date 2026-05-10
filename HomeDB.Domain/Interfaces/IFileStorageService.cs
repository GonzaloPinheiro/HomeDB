
namespace HomeDB.Domain.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un stream en disco con el nombre indicado.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="storedName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Elimina el fichero físico del disco si existe.
        /// </summary>
        /// <param name="storedName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(string storedName, CancellationToken cancellationToken);

        /// <summary>
        /// Comprueba si el fichero existe en disco.
        /// </summary>
        /// <param name="storedName"></param>
        /// <returns></returns>
        bool Exists(string storedName);
    }
}