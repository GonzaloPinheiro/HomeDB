
namespace HomeDB.Domain.Common
{
    public static class AllowedExtensions
    {
        public static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            // Imágenes
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            // Vídeo
            ".mp4", ".mov",
            // Audio
            ".mp3", ".wav", ".flac",
            // Documentos
            ".pdf", ".docx", ".xlsx", ".txt", ".md", ".csv",
            // Comprimidos
            ".zip", ".7z", ".rar",
            // Código y datos
            ".json", ".xml", ".yaml", ".yml"
        };
    }
}