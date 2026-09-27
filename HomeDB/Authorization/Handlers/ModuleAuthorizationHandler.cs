using HomeDB.Application.Services;
using HomeDB.Authorization.Requirements;
using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace HomeDB.Authorization.Handlers
{
    public class ModuleAuthorizationHandler : AuthorizationHandler<ModuleRequirement>
    {
        //Variables y objetos globales
        private readonly UserModulePermissionsService _userModulePermissionsService;
        private readonly ICurrentUserService _currentUserService;

        //Constructores
        public ModuleAuthorizationHandler(UserModulePermissionsService userModulePermissionsService, ICurrentUserService currentUserService)
        {
            _userModulePermissionsService = userModulePermissionsService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Verifica si el usuario tiene permisos para acceder al módulo especificado en el requisito.
        /// </summary>
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

            //Verifica si el módulo está habilitado para el usuario a través del servicio de aplicación
            bool hasAccess = await _userModulePermissionsService.HasAccessAsync(userId, requirement.Module, CancellationToken.None);

            //Si el módulo está habilitado, se cumple el requisito; de lo contrario, se falla la autorización
            if (hasAccess)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
