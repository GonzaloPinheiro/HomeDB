using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;
using MimeDetective;
using MimeDetective.Definitions;

namespace HomeDB.Infrastructure.Services
{
    /// <summary>
    /// Implementación de IFileTypeValidator usando MimeDetective para validar que el contenido del archivo coincide con su extensión declarada.
    /// </summary>
    public class MimeDetectiveFileTypeValidator : IFileTypeValidator
    {
        private static readonly IContentInspector Inspector = new ContentInspectorBuilder()
        {
            Definitions = new ExhaustiveBuilder().Build()
        }.Build();

        public bool IsValid(string fileName, byte[] headerBytes)
        {
            string extension = Path.GetExtension(fileName);

            if (!AllowedExtensions.Whitelist.Contains(extension))
                return false;

            IReadOnlyCollection<MimeDetective.Engine.DefinitionMatch> matches = Inspector.Inspect(headerBytes);

            if (matches.Count == 0)
                return false;

            return matches.Any(m => m.Definition.File.Extensions.Contains(
                extension.TrimStart('.'),
                StringComparer.OrdinalIgnoreCase));
        }
    }
}