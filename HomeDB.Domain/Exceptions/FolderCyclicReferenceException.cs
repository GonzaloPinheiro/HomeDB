
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Mover el folder al destino indicado crearía un ciclo en el árbol de carpetas.
    /// </summary>
    public class FolderCyclicReferenceException : Exception
    {
        public FolderCyclicReferenceException(int folderId)
            : base($"Moving folder {folderId} to the specified destination would create a cyclic reference.") { }
    }
}
