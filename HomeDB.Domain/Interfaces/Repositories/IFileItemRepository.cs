using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IFileItemRepository
    {
        /// <summary>
        /// Agrega un nuevo FileItem a la base de datos
        /// </summary>
        /// <param name="fileItem"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task AddAsync(FileItem fileItem, CancellationToken cToken);

        /// <summary>
        /// Busca un FileItem por su Id. Retorna null si no se encuentra
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task<FileItem?> GetByIdAsync(int id, CancellationToken cToken);

        /// <summary>
        /// Retorna los archivos del usuario en la carpeta indicada.
        /// Sin folderId devuelve los archivos de la raíz.
        /// </summary>
        Task<IEnumerable<FileItem>> GetByOwnerAndFolderAsync(int ownerId, int? folderId, CancellationToken cToken);

        /// <summary>
        /// Elimina el fileItem recibido como parámetro de la base de datos
        /// </summary>
        /// <param name="fileItem"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        void DeleteFile(FileItem fileItem);

        /// <summary>
        /// Confirma los cambios sobre la base de datos
        /// </summary>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}
