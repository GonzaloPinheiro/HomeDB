
namespace HomeDB.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        //Nombre rol
        public string RoleName { get; set; } = string.Empty;
        //Descripción del rol
        public string RoleDescription { get; set; } = string.Empty;

        //Navigation property — relaciones asignadas a este rol
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
