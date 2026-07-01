
namespace HomeDB.Application.DTOs
{
    public class UpdateModulePermissionsRequestDto
    {
        public bool? FilesEnabled { get; set; }
        public bool? ExpensesEnabled { get; set; }
        public bool? InvestmentsEnabled { get; set; }
        public bool? SystemMonitorEnabled { get; set; }
        public bool? UserManagementEnabled { get; set; }
        public bool? RoleManagementEnabled { get; set; }
        public bool? SystemLogsEnabled { get; set; }
        public bool? AuditLogsEnabled { get; set; }
        public bool? RemoteScriptsEnabled { get; set; }
    };

    public record UserModulePermissionsResponseDto(
        bool FilesEnabled,
        bool ExpensesEnabled,
        bool InvestmentsEnabled,
        bool SystemMonitorEnabled,
        bool UserManagementEnabled,
        bool RoleManagementEnabled,
        bool SystemLogsEnabled,
        bool AuditLogsEnabled,
        bool RemoteScriptsEnabled
    );
}