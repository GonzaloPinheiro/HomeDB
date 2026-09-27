namespace HomeDB.Domain.Interfaces.Services
{
    public interface IContentTypeResolver
    {
        /// <summary>
        /// Intenta resolver el content-type MIME de un archivo a partir de su nombre/extensión.
        /// </summary>
        bool TryGetContentType(string fileName, out string? contentType);
    }
}
