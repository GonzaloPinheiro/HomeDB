
namespace HomeDB.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Navigation property — roles asignados a este usuario
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public UserModulePermissions? ModulePermissions { get; set; } //Modulos de usuario
        public UserSettings? Settings { get; set; } //Configuración de usuario
        public UserAdminSettings? AdminSettings { get; set; } //Configuración de usuario(modificable solo por administradores)
    }
}
