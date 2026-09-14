
namespace HomeDB.Domain.Interfaces
{

    public interface IUploadChunkLockProvider
    {
        /// <summary>
        /// Obtiene un lock para un chunk específico de una sesión de carga.
        /// </summary>
        SemaphoreSlim GetLock(Guid sessionId, int chunkNumber);

        /// <summary>
        /// Libera todos los locks (de chunks y de finalización) asociados a una sesión de carga.
        /// Debe llamarse cuando la sesión alcanza un estado terminal persistido (Completed o Cancelled) para evitar que se acumulen en memoria.
        /// </summary>
        void ReleaseSessionLocks(Guid sessionId);
    }
}