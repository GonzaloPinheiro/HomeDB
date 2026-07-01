
namespace HomeDB.Domain.Entities
{
    public class UserAdminSettings
    {
        public int Id { get; set; } // Primary key
        public int UserId { get; set; } //Clave foranea a la tabla de usuarios
        public User User { get; set; } = null!; // Navigation property hacia la entidad User

        public long? StorageLimitBytes { get; set; } = null; // Indica el límite de almacenamiento en bytes para el usuario. Si es null, se usa el de appsettings
        public long? MaxFileSizeBytes { get; set; } = null; // Indica el tamaño máximo de archivo en bytes para el usuario. Si es null, se usa el de appsettings
    }
}