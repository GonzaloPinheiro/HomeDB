
namespace HomeDB.Domain.Common.Enums
{
    //Lista de módulos del sistema. Cada módulo representa una funcionalidad o área específica de la aplicación.
    //Cuando se agregue un nuevo módulo, se debe actualizar este enum, también la tabla de módulos en la base de datos y el switch del HandleRequirementAsync.
    public enum AppModules
    {
        Files, // Módulo de gestión de archivos
        Expenses, // Módulo de gestión de gastos
        Investments, // Módulo de gestión de inversiones
        SystemMonitor, // Módulo de monitorización del sistema
        UserManagement, // Módulo de gestión de usuarios
        RoleManagement, // Módulo de gestión de roles
        SystemLogs, // Módulo de registros del sistema
        AuditLogs, // Módulo de registros de auditoría
        RemoteScripts // Módulo de scripts remotos
    }
}