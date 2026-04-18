
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Intento de borrar una carpeta que tiene contenido
    /// </summary>
    public class FolderNotEmptyException : Exception
    {
        public FolderNotEmptyException(int id)
            : base($"Folder with id {id} is not empty and cannot be deleted.") { }
    }
}