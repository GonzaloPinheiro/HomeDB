
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Archivo no encontrado en base de datos
    /// </summary>
    public class FileNotFoundException : Exception
    {
        public FileNotFoundException(int id)
            : base($"File with id {id} was not found.") { }
    }
}