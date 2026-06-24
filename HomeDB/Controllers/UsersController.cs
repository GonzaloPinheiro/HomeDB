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
    [Authorize(Roles = nameof(RolesList.Admin))]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    [Route("api/admin")]
    public class UsersController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly Logger _logger;
        private readonly UsersService _usersService;

        //Constuctores
        public UsersController(Logger logger, UsersService usersService)
        {
            _logger = logger;
            _usersService = usersService;
        }

        [HttpGet]
        [Route("users")]
        public async Task<IActionResult> GetUsersAsync([FromQuery] GetUsersRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UsersController",
                operation: "GetUsersAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener la lista de usuarios
            GetUsersResponseDto result = await _usersService.GetUsersAsync(dto, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<GetUsersResponseDto>.Success(result));
        }

        [HttpGet]
        [Route("users/{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(int userId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int currentUserId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UsersController",
                operation: "GetUserByIdAsync()",
                correlationId: correlationId,
                userId: currentUserId.ToString());

            //Obtener el usuario por su ID
            UserSummaryDto? result = await _usersService.GetUserByIdAsync(userId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<UserSummaryDto?>.Success(result));
        }

        [HttpDelete]
        [Route("users/{userId}")]
        public async Task<IActionResult> DeleteUserAsync(int userId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int requestingUserId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UsersController",
                operation: "DeleteUserAsync()",
                correlationId: correlationId,
                userId: requestingUserId.ToString());

            //Eliminar el usuario
            DeleteUserResponseDto result = await _usersService.DeleteUserAsync(userId, requestingUserId, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<DeleteUserResponseDto>.Success(result));
        }
    }
}