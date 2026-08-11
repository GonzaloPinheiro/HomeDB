using HomeDB.Domain.Interfaces;
using System.Collections.Concurrent;

namespace HomeDB.Infrastructure.Storage
{
    public class UploadChunkLockProvider : IUploadChunkLockProvider
    {
        //Diccionario anidado: por cada sesión, un diccionario de locks por número de chunk.
        //Permite liberar de una sola vez todos los locks de una sesión al finalizar, sin tener que iterar ni parsear claves.
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, SemaphoreSlim>> _locks = new ConcurrentDictionary<Guid, ConcurrentDictionary<int, SemaphoreSlim>>();

        //Devuelve un SemaphoreSlim para un chunk específico de una sesión de carga. Si no existe, se crea uno nuevo.
        public SemaphoreSlim GetLock(Guid sessionId, int chunkNumber)
        {
            ConcurrentDictionary<int, SemaphoreSlim> sessionLocks = _locks.GetOrAdd(sessionId, _ => new ConcurrentDictionary<int, SemaphoreSlim>());
            return sessionLocks.GetOrAdd(chunkNumber, _ => new SemaphoreSlim(1, 1));
        }

        //Elimina todos los locks de una sesión (chunks + finalización) de una sola vez.
        public void ReleaseSessionLocks(Guid sessionId)
        {
            _locks.TryRemove(sessionId, out _);
        }
    }
}