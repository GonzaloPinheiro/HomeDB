using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IFolderRepository
    {
        /// <summary>
        /// Devuelve un folder por su id, o null si no existe.
        /// </summary>
        Task<FolderItem?> GetByIdAsync(int folderId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve una lista de folders que son hijos de un folder padre.
        /// </summary>
        Task<IEnumerable<FolderItem>> GetByParentAsync(int ownerId, int? parentFolderId, CancellationToken cToken);

        /// <summary>
        /// Crea un nuevo folder en la base de datos.
        /// </summary>
        Task CreateAsync(FolderItem folderItem, CancellationToken cToken);

        /// <summary>
        /// Elimina un folder de la base de datos.
        /// </summary>
        Task DeleteAsync(FolderItem folderItem, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);

        /// <summary>
        /// Comprueba si un folder tiene archivos asociados.
        /// </summary>
        Task<bool> HasFilesAsync(int folderId, CancellationToken cToken);

        /// <summary>
        /// Comprueba si un folder tiene subfolders asociados.
        /// </summary>
        Task<bool> HasSubfoldersAsync(int folderId, CancellationToken cToken);

        /// <summary>
        /// Comprueba si un usuario tiene folders asociados en la base de datos.
        /// </summary>
        Task<bool> UserHasFoldersAsync(int ownerId, CancellationToken cToken);

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