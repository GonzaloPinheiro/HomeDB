using HomeDB.Domain.Interfaces;
using System.Collections.Concurrent;

namespace HomeDB.Infrastructure.Storage
{
    public class UploadChunkLockProvider : IUploadChunkLockProvider //TODO REVIASAR
    {
        //ConcurrentDictionary encargado de almacenar los locks para cada chunk de cada sesión de carga. La clave es una combinación del sessionId y el chunkNumber.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        //Devuelve un SemaphoreSlim para un chunk específico de una sesión de carga. Si no existe, se crea uno nuevo.
        public SemaphoreSlim GetLock(Guid sessionId, int chunkNumber)
        {
            string key = $"{sessionId}_{chunkNumber}";
            return _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }
    }
}