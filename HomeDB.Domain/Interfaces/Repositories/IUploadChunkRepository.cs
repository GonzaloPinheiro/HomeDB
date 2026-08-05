using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUploadChunkRepository
    {
        /// <summary>
        /// Verifica si un chunk de subida ya existe en el repositorio.
        /// </summary>
        Task<bool> ExistsAsync(int uploadSessionId, int chunkNumber, CancellationToken cToken);

        /// <summary>
        /// Agrega un nuevo chunk de subida al repositorio.
        /// </summary>
        Task AddAsync(UploadChunk chunk, CancellationToken cToken);

        /// <summary>
        /// Devuelve una lista de números de chunks recibidos para una sesión de subida específica.
        /// </summary>
        /// <param name="uploadSessionId"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task<List<int>> GetReceivedChunkNumbersAsync(int uploadSessionId, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en el repositorio de chunks de subida.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}