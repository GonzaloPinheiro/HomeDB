
namespace HomeDB.Domain.Entities
{
    public class FolderItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }  // null = carpeta raíz
        public FolderItem? ParentFolder { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
