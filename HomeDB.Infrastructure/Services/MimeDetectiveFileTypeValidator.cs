using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;
using MimeDetective;
using MimeDetective.Definitions;

namespace HomeDB.Infrastructure.Services
{
    /// <summary>
    /// Implementación de IFileTypeValidator que valida que el contenido del archivo coincide con su extensión declarada.
    /// Según el tipo usa una de tres estrategias:
    /// - Formatos con firma (magic numbers): MimeDetective.
    /// - Formatos sin firma posible (texto plano y derivados): se comprueba que el contenido sea texto, no binario.
    /// - Formatos que MimeDetective no cubre bien con la cabecera sola (OOXML, MP3): reglas propias.
    /// </summary>
    public class MimeDetectiveFileTypeValidator : IFileTypeValidator
    {
        //~4MB de footprint, cubre los formatos binarios de AllowedExtensions.Whitelist
        private static readonly IContentInspector Inspector = new ContentInspectorBuilder()
        {
            Definitions = DefaultDefinitions.All()
        }.Build();

        //Formatos de texto: no existe magic number, así que solo se puede comprobar que el contenido no sea binario.
        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".md", ".csv", ".json", ".xml", ".yaml", ".yml"
        };

        //OOXML son zips: con la cabecera solo se puede confirmar que es un zip, no que sea Word o Excel.
        private static readonly HashSet<string> ZipContainerExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".docx", ".xlsx"
        };

        public bool IsValid(string fileName, byte[] headerBytes)
        {
            string extension = Path.GetExtension(fileName);

            if (!AllowedExtensions.Whitelist.Contains(extension))
                return false;

            //Texto: sin firma posible. Un byte NUL delata un binario renombrado (ejecutables, imágenes, zips...).
            if (TextExtensions.Contains(extension))
                return !headerBytes.Contains((byte)0);

            //Los binarios necesitan siempre alguna cabecera: un archivo vacío no puede ser un PNG o un PDF.
            if (headerBytes.Length == 0)
                return false;

            //MP3: lo cubre una regla propia porque MimeDetective no reconoció las cabeceras habituales en las pruebas.
            if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                return IsMp3(headerBytes);

            IReadOnlyCollection<MimeDetective.Engine.DefinitionMatch> matches = Inspector.Inspect(headerBytes);

            if (matches.Count == 0)
                return false;

            string extensionWithoutDot = extension.TrimStart('.');

            if (matches.Any(m => m.Definition.File.Extensions.Contains(extensionWithoutDot, StringComparer.OrdinalIgnoreCase)))
                return true;

            //docx/xlsx: se acepta si el contenido es un zip (firma PK).
            if (ZipContainerExtensions.Contains(extension))
                return matches.Any(m => m.Definition.File.Extensions.Contains("zip", StringComparer.OrdinalIgnoreCase));

            return false;
        }

        /// <summary>
        /// MP3 válido: empieza por una etiqueta ID3v2 ("ID3") o por un frame MPEG (11 bits de sincronización a 1).
        /// </summary>
        private static bool IsMp3(byte[] header)
        {
            if (header.Length >= 3 && header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33)
                return true;

            return header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0;
        }
    }
}
