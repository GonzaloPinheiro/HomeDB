
namespace HomeDB.Domain.Interfaces.Services
{
    /// <summary>
    /// Valida que el contenido de un archivo coincide con su extensión declarada.
    /// </summary>
    public interface IFileTypeValidator
    {
        bool IsValid(string fileName, byte[] headerBytes);
    }
}