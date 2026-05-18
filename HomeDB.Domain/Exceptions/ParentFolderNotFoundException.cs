
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// La carpeta padre no existe o no pertenece al usuario
    /// </summary>
    public class ParentFolderNotFoundException : Exception
    {
        public ParentFolderNotFoundException(int parentFolderId)
            : base($"Parent folder with id {parentFolderId} was not found or does not belong to the user.") { }
    }
}