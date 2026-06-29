using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IFolderRepository
    {
        /// <summary>
        /// Devuelve un folder por su id, o null si no existe.
        /// </summary>
        /// <param name="folderId"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task<FolderItem?> GetByIdAsync(int folderId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve una lista de folders que son hijos de un folder padre.
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="parentFolderId"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task<IEnumerable<FolderItem>> GetByParentAsync(int ownerId, int? parentFolderId, CancellationToken cToken);

        /// <summary>
        /// Crea un nuevo folder en la base de datos.
        /// </summary>
        /// <param name="folderItem"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task CreateAsync(FolderItem folderItem, CancellationToken cToken);

        /// <summary>
        /// Elimina un folder de la base de datos.
        /// </summary>
        /// <param name="folderItem"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task DeleteAsync(FolderItem folderItem, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cToken);

        Task<bool> HasFilesAsync(int folderId, CancellationToken cToken);
        Task<bool> HasSubfoldersAsync(int folderId, CancellationToken cToken);

        /// <summary>
        /// Devuelve true si potentialDescendantId es descendiente de folderId (o el propio folderId).
        /// Implementación con CTE recursiva (una sola query).
        /// </summary>
        Task<bool> IsDescendantAsync(int folderId, int potentialDescendantId, CancellationToken cToken);

        /// <summary>
        /// Devuelve true si potentialDescendantId es descendiente de folderId (o el propio folderId).
        /// Implementación iterativa: sube por el árbol desde potentialDescendantId hasta la raíz (una query por nivel).
        /// </summary>
        Task<bool> IsDescendantAsync2(int folderId, int potentialDescendantId, CancellationToken cToken);
    }
}