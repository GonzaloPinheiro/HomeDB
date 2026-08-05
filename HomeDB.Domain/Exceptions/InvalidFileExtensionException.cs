
namespace HomeDB.Domain.Exceptions
{
    //Usada para indicar cuando se intenta subir un archivoq que no entra dentro de la whitelist
    public class InvalidFileExtensionException : Exception
    {
        public InvalidFileExtensionException(string extensio)
            : base($"The extension '{extensio}' is not a valid one.") { }
    }
}