
namespace HomeDB.Domain.Entities
{
    public class FolderItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }  // null = carpeta raíz
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public User? Owner { get; set; }
        public FolderItem? ParentFolder { get; set; }
    }
}
