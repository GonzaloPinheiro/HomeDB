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
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    [Authorize(Roles = nameof(RolesList.Admin))]
    [Route("api/admin/roles")]
    public class RolesController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly Logger _logger;
        private readonly RolesService _rolesService;

        //Constructores
        public RolesController(Logger logger, RolesService rolesService)
        {
            _logger = logger;
            _rolesService = rolesService;
        }

        [HttpGet]
        [Route("{roleId}")]
        public async Task<IActionResult> GetRoleAsync([FromRoute] int roleId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.RolesController",
                operation: "GetRoleAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el rol
            RoleResponseDto result = await _rolesService.GetRoleAsync(roleId, cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<RoleResponseDto>.Success(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesAsync(CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.RolesController",
                operation: "GetRolesAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener la lista de roles
            IEnumerable<RoleResponseDto> result = await _rolesService.GetRolesAsync(cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<IEnumerable<RoleResponseDto>>.Success(result));
        }


        [HttpPatch]
        [Route("{roleId}/description")]
        public async Task<IActionResult> UpdateRoleDescriptionAsync([FromRoute] int roleId, [FromQuery] string newDescription, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.RolesController",
                operation: "UpdateRoleDescriptionAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Actualizar la descripción del rol
            RoleResponseDto result = await _rolesService.UpdateDescriptionAsync(roleId, newDescription, cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<RoleResponseDto>.Success(result));
        }
    }
}