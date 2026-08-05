using System.Collections.Concurrent;

namespace HomeDB.Domain.Interfaces
{

    public interface IUploadChunkLockProvider
    {
        /// <summary>
        /// Obtiene un lock para un chunk específico de una sesión de carga.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="chunkNumber"></param>
        /// <returns></returns>
        SemaphoreSlim GetLock(Guid sessionId, int chunkNumber);
    }
}