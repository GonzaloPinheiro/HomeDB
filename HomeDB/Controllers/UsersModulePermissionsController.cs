using HomeDB.Application.DTOs;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Route("api")]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    public class UsersModulePermissionsController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly UserModulePermissionsService _permissionsService;
        private readonly Logger _logger;

        //Constructores
        public UsersModulePermissionsController(UserModulePermissionsService permissionsService, Logger logger)
        {
            _permissionsService = permissionsService;
            _logger = logger;
        }

        [HttpGet("users/me/permissions")]
        [Authorize]
        public async Task<IActionResult> GetMyPermissionsAsync(CancellationToken cToken)
        {
            //Variables y objetos
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UserModulePermissionsController",
                operation: "GetMyPermissionsAsync()",
                correlationId: GetCorrelationId(),
                userId: userId.ToString());

            //Obtener los permisos del usuario actual
            UserModulePermissionsResponseDto dto = await _permissionsService.GetPermissionsForCurrentUserAsync(cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserModulePermissionsResponseDto>.Success(dto));
        }


        [HttpPatch("admin/users/{id:int}/permissions")]
        [Authorize(Roles = nameof(RolesList.Admin))]
        public async Task<IActionResult> UpdatePermissionsAsync(int id, [FromBody] UpdateModulePermissionsRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UserModulePermissionsController",
                operation: "UpdatePermissionsAsync()",
                correlationId: GetCorrelationId(),
                userId: userId.ToString());

            //Actualizar los permisos del usuario especificado
            UserModulePermissionsResponseDto result = await _permissionsService.UpdatePermissionsAsync(id, dto, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserModulePermissionsResponseDto>.Success(result));
        }
    }
}