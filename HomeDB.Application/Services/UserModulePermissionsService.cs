using HomeDB.Application.DTOs;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class UserModulePermissionsService
    {
        //Variables y objetos globales
        private readonly IUserModulePermissionsRepository _permissionsRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;

        //Constructores
        public UserModulePermissionsService(IUserModulePermissionsRepository permissionsRepository,IUserRepository userRepository,ICurrentUserService currentUserService)
        {
            _permissionsRepository = permissionsRepository;
            _userRepository = userRepository;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Obtiene los permisos de módulos del usuario actual.
        /// </summary>
        public async Task<UserModulePermissionsResponseDto> GetPermissionsForCurrentUserAsync(CancellationToken cToken)
        {
            //Obtener el ID del usuario actual desde el servicio de usuario actual
            int userId = _currentUserService.UserId;

            //Obtener la lista de modulos del usuario actual desde el repositorio de permisos
            UserModulePermissions permissions = await _permissionsRepository.GetByUserIdAsync(userId, cToken)
                ?? throw new UserModulePermissionsNotFoundException(userId);

            //Devolver el DTO de respuesta mapeado desde la entidad UserModulePermissions
            return MapToDto(permissions);
        }

        /// <summary>
        /// Obtiene los permisos de módulos de un usuario específico.
        /// </summary>
        public async Task<UserModulePermissionsResponseDto> GetPermissionsByUserIdAsync(int userId, CancellationToken cToken)
        {
            //Verificar que el usuario existe
            User? user = await _userRepository.GetUserByIdAsync(userId, cToken)
                ?? throw new UserNotFoundException(userId);

            //Obtener los permisos de módulos del usuario desde el repositorio
            UserModulePermissions permissions = await _permissionsRepository.GetByUserIdAsync(userId, cToken)
                ?? throw new UserModulePermissionsNotFoundException(userId);

            //Devolver el DTO de respuesta mapeado desde la entidad UserModulePermissions
            return MapToDto(permissions);
        }

        /// <summary>
        /// Actualiza los permisos de módulos de un usuario específico.
        /// </summary>
        public async Task<UserModulePermissionsResponseDto> UpdatePermissionsAsync(int userId, UpdateModulePermissionsRequestDto dto, CancellationToken cToken)
        {
            //Obtener el usuario desde el repositorio de usuarios
            User? user = await _userRepository.GetUserByIdAsync(userId, cToken)
                ?? throw new UserNotFoundException(userId);

            //Obtiene los permisos de módulos del usuario
            UserModulePermissions permissions = await _permissionsRepository.GetByUserIdAsync(userId, cToken, asNoTracking: false)
                ?? throw new UserModulePermissionsNotFoundException(userId);

            //Modifica los permisos de módulos si no son nulos en el DTO de solicitud
            if (dto.FilesEnabled.HasValue) permissions.FilesEnabled = dto.FilesEnabled.Value;
            if (dto.ExpensesEnabled.HasValue) permissions.ExpensesEnabled = dto.ExpensesEnabled.Value;
            if (dto.InvestmentsEnabled.HasValue) permissions.InvestmentsEnabled = dto.InvestmentsEnabled.Value;
            if (dto.SystemMonitorEnabled.HasValue) permissions.SystemMonitorEnabled = dto.SystemMonitorEnabled.Value;
            if (dto.UserManagementEnabled.HasValue) permissions.UserManagementEnabled = dto.UserManagementEnabled.Value;
            if (dto.RoleManagementEnabled.HasValue) permissions.RoleManagementEnabled = dto.RoleManagementEnabled.Value;
            if (dto.SystemLogsEnabled.HasValue) permissions.SystemLogsEnabled = dto.SystemLogsEnabled.Value;
            if (dto.AuditLogsEnabled.HasValue) permissions.AuditLogsEnabled = dto.AuditLogsEnabled.Value;
            if (dto.RemoteScriptsEnabled.HasValue) permissions.RemoteScriptsEnabled = dto.RemoteScriptsEnabled.Value;

            //Persistir los cambios en la base de datos
            await _permissionsRepository.SaveChangesAsync(cToken);

            //Devolver el resultado mapeado a DTO
            return MapToDto(permissions);
        }

        /// <summary>
        /// Crea los permisos de módulos predeterminados para un nuevo usuario.
        /// </summary>
        public async Task CreateDefaultPermissionsAsync(int userId, CancellationToken cToken)
        {
            UserModulePermissions permissions = new UserModulePermissions
            {
                UserId = userId
                //Todos los modulos son false por defecto
            };

            await _permissionsRepository.AddAsync(permissions, cToken);
            // SaveChanges lo hace AuthService en la misma transacción
        }

        /// <summary>
        /// Verifica si un módulo específico está habilitado para el usuario actual y lanza una excepción si no lo está.
        /// </summary>
        public void EnsureModuleEnabled(UserModulePermissions permissions, string moduleName, Func<UserModulePermissions, bool> selector)
        {
            if (!selector(permissions))
                throw new ModuleAccessDeniedException(moduleName);
        }

        /// <summary>
        /// Mapeo de la entidad UserModulePermissions a UserModulePermissionsResponseDto
        /// </summary>
        private static UserModulePermissionsResponseDto MapToDto(UserModulePermissions p)
        {
            return new UserModulePermissionsResponseDto(
                FilesEnabled: p.FilesEnabled,
                ExpensesEnabled: p.ExpensesEnabled,
                InvestmentsEnabled: p.InvestmentsEnabled,
                SystemMonitorEnabled: p.SystemMonitorEnabled,
                UserManagementEnabled: p.UserManagementEnabled,
                RoleManagementEnabled: p.RoleManagementEnabled,
                SystemLogsEnabled: p.SystemLogsEnabled,
                AuditLogsEnabled: p.AuditLogsEnabled,
                RemoteScriptsEnabled: p.RemoteScriptsEnabled
            );
        }
    }
}
