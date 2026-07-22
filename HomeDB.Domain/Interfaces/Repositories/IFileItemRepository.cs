using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IFileItemRepository
    {
        /// <summary>
        /// Agrega un nuevo FileItem a la base de datos
        /// </summary>
        Task AddAsync(FileItem fileItem, CancellationToken cToken);

        /// <summary>
        /// Busca un FileItem por su Id. Retorna null si no se encuentra
        /// </summary>
        Task<FileItem?> GetByIdAsync(int id, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Busca un FileItem aplicando los filtros recibidos
        /// </summary>
        Task<(IEnumerable<FileItem> Items, int TotalCount)> SearchFileAsync(string? fileName, int ownerId, int? folderId, string? contentType,
                                                    long? minSizeBytes, long? maxSizeBytes, DateTime? uploadedFrom, DateTime? uploadedTo,
                                                    int pageNumber, int pageSize, CancellationToken cToken);

        /// <summary>
        /// Retorna los archivos del usuario en la carpeta indicada.
        /// Sin folderId devuelve los archivos de la raíz.
        /// </summary>
        Task<IEnumerable<FileItem>> GetByOwnerAndFolderAsync(int ownerId, int? folderId, CancellationToken cToken);

        /// <summary>
        /// Retorna las estadísticas de almacenamiento para el usuario especificado.
        /// </summary>
        Task<(int TotalFiles, long TotalSizeBytes, int TotalFolders)> GetUserStatsAsync(int ownerId, CancellationToken cToken);

        /// <summary>
        /// Elimina el fileItem recibido como parámetro de la base de datos
        /// </summary>
        void DeleteFile(FileItem fileItem);

        /// <summary>
        /// Comprueba si el usuario tiene archivos asociados en la base de datos
        /// </summary>
        Task<bool> UserHasFilesAsync(int ownerId, CancellationToken cToken);

        /// <summary>
        /// Confirma los cambios sobre la base de datos
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}