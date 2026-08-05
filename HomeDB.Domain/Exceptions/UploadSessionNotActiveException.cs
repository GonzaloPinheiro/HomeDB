
namespace HomeDB.Domain.Exceptions
{
    public class UploadSessionNotActiveException : Exception
    {
        public UploadSessionNotActiveException(string sessionId) 
            : base($"Upload session not active: {sessionId}"){}
    }
}