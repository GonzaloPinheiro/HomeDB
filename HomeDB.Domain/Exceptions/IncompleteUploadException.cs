
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Indica que se intentó acceder a un archivo de carga incompleto.
    /// </summary>
    public class IncompleteUploadException : Exception
    {
        public IncompleteUploadException(string message) 
            :base($"Incomplete upload: {message}") { }

        public IncompleteUploadException(Guid sessionId, int receivedCount, int totalChunks)
          : base($"Upload session {sessionId} is incomplete. Received {receivedCount} of {totalChunks} chunks.") { }
    }
}