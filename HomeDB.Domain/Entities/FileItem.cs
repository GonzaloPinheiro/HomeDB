
namespace HomeDB.Domain.Entities
{
    //Representa un archivo subido por un usuario: sus metadatos y su ubicación en el almacenamiento y en el árbol de carpetas.
    public class FileItem
    {
        public int Id { get; set; } //PK
        public string FileName { get; set; } = string.Empty;   // Nombre original
        public string StoredName { get; set; } = string.Empty; // GUID en disco
        public long SizeBytes { get; set; } // Tamaño del archivo en bytes
        public string ContentType { get; set; } = string.Empty; // Content-Type/MIME del archivo
        public int? FolderId { get; set; } // Id de la carpeta donde se almacena (null = raíz)
        public int OwnerId { get; set; } // Id del usuario propietario del archivo

        public FolderItem? Folder { get; set; } // Navigation property hacia la carpeta donde se almacena
        public User? Owner { get; set; } // Navigation property hacia el usuario propietario
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow; // Fecha y hora en que se subió el archivo
    }
}