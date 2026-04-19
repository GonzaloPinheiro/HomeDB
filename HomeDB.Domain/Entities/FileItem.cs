
namespace HomeDB.Domain.Entities
{
    public class FileItem
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;   // nombre original
        public string StoredName { get; set; } = string.Empty; // GUID en disco
        public long SizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int? FolderId { get; set; }
        public FolderItem? Folder { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
