
namespace HomeDB.Domain.Exceptions
{
    //Excepción usada para indicar que los datos de inicio de una sesión de subida son inválidos.
    public class InvalidUploadRequestException : Exception
    {
        public InvalidUploadRequestException(string message) : base(message) { }
    }
}
