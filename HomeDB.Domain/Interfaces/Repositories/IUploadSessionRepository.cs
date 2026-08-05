using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUploadSessionRepository
    {
        /// <summary>
        /// Agrega una nueva sesión de subida al repositorio.
        /// </summary>
        Task<UploadSession> AddAsync(UploadSession session, CancellationToken cToken);

        /// <summary>
        /// Busca una sesión de subida por su SessionId.
        /// </summary>
        Task<UploadSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Busca todas las sesiones de subida que han sido finalizadas.
        /// </summary>
        Task<List<UploadSession>> GetFinishedSessionsAsync(CancellationToken cToken);

        /// <summary>
        /// Incrementa atómicamente ReceivedSizeBytes si el resultado no supera maxSizeBytes.
        /// Devuelve true si se actualizó correctamente, false si se superaría el límite.
        /// </summary>
        Task<bool> TryIncrementReceivedSizeBytesAsync(int sessionId, long chunkSizeBytes, long maxSizeBytes, CancellationToken cToken);

        /// <summary>
        /// Elimina una sesión de subida del repositorio.
        /// </summary>
        void RemoveRange(IEnumerable<UploadSession> sessions);

        /// <summary>
        /// Periste los cambios realizados en el repositorio de sesiones de subida.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}