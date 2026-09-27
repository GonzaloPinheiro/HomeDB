
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Carpeta no encontrada en base de datos
    /// </summary>
    public class FolderNotFoundException : Exception
    {
        public FolderNotFoundException(int id)
            : base($"Folder with id {id} was not found.") { }
    }
}