
namespace HomeDB.Domain.Entities
{
    //Entidad que representa en que módulos tiene permisos un usuario.
    public class UserModulePermissions
    {
        public int Id { get; set; } //Clave primaria
        public int UserId { get; set; } //Clave foránea hacia la entidad User
        public User User { get; set; } = null!; //Propiedad de navegación hacia la entidad User

        //Módulos de usuario
        public bool FilesEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de archivos
        public bool ExpensesEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de gastos
        public bool InvestmentsEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de inversiones

        //Módulos de Admin
        public bool SystemMonitorEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de monitorización del sistema
        public bool UserManagementEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de gestión de usuarios
        public bool RoleManagementEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de gestión de roles
        public bool SystemLogsEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de logs del sistema
        public bool AuditLogsEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de logs de auditoría
        public bool RemoteScriptsEnabled { get; set; } = false; //Indica si el usuario tiene acceso al módulo de ejecución remota de scripts
    }
}