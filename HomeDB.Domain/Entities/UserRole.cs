
namespace HomeDB.Domain.Entities
{
    public class UserRole
    {
        public int Id { get; set; }
        //Id del usuario
        public int UserId { get; set; }
        //Id del rol asignado al usuario
        public int RoleId { get; set; }

        //ForeignKeys
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
