
using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces
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
        /// Confirma los cambios sobre la base de datos
        /// </summary>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}
