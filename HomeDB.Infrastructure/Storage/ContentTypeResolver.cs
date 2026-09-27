using HomeDB.Domain.Interfaces.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace HomeDB.Infrastructure.Storage
{
    public class ContentTypeResolver : IContentTypeResolver
    {
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        public bool TryGetContentType(string fileName, out string? contentType)
        {
            return _contentTypeProvider.TryGetContentType(fileName, out contentType);
        }
    }
}
