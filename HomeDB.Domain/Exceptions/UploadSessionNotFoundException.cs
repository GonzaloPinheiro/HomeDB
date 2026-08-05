
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Indica que no se encontró una sesión de upload con el identificador proporcionado.
    /// </summary>
    public class UploadSessionNotFoundException : Exception
    {
        public UploadSessionNotFoundException(string sessionId) 
            :base($"Upload session not found: {sessionId}"){}
    }
}