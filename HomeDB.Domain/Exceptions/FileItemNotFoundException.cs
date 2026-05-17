
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Archivo no encontrado en base de datos
    /// </summary>
    public class FileItemNotFoundException : Exception
    {
        public FileItemNotFoundException(int id)
            : base($"File with id {id} was not found.") { }
    }
}