using HomeDB.Application.Authorization.Requirements;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace HomeDB.Application.Authorization.Handlers
{
    public class ModuleAuthorizationHandler : AuthorizationHandler<ModuleRequirement>
    {
        //Variables y objetos globales
        private readonly IUserModulePermissionsRepository _permissionsRepository;
        private readonly ICurrentUserService _currentUserService;

        //Constructores
        public ModuleAuthorizationHandler(IUserModulePermissionsRepository permissionsRepository, ICurrentUserService currentUserService)
        {
            _permissionsRepository = permissionsRepository;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Verifica si el usuario tiene permisos para acceder al módulo especificado en el requisito.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuleRequirement requirement)
        {
            //Verifica si el usuario está autenticado
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return;
            }

            //El rol Admin es el superusuario del sistema y no se ve limitado por los permisos de los módulos.
            if (context.User.IsInRole(nameof(RolesList.Admin)))
            {
                context.Succeed(requirement);
                return;
            }

            //Obtiene el ID del usuario actual
            int userId = _currentUserService.UserId;

            //Obtiene los permisos del usuario para el módulo especificado
            UserModulePermissions? permissions = await _permissionsRepository
                .GetByUserIdAsync(userId, CancellationToken.None);

            //Si no se encuentran permisos, se falla la autorización
            if (permissions is null)
            {
                context.Fail();
                return;
            }

            //Verifica si el módulo está habilitado para el usuario
            bool isEnabled = requirement.Module switch
            {
                AppModules.Files => permissions.FilesEnabled,
                AppModules.Expenses => permissions.ExpensesEnabled,
                AppModules.Investments => permissions.InvestmentsEnabled,
                AppModules.SystemMonitor => permissions.SystemMonitorEnabled,
                AppModules.UserManagement => permissions.UserManagementEnabled,
                AppModules.RoleManagement => permissions.RoleManagementEnabled,
                AppModules.SystemLogs => permissions.SystemLogsEnabled,
                AppModules.AuditLogs => permissions.AuditLogsEnabled,
                AppModules.RemoteScripts => permissions.RemoteScriptsEnabled,
                _ => false
            };

            //Si el módulo está habilitado, se cumple el requisito; de lo contrario, se falla la autorización
            if (isEnabled)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}