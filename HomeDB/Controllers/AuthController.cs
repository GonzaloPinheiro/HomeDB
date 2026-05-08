using HomeDB.Application.DTOs.Auth;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Mvc;

namespace HomeDB.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly Logger _logger;
        private readonly AuthService _authService;

        public AuthController(Logger logger, AuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }


        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync(RegisterDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown"; // username del JWT

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "RegisterAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            UserDto result = await _authService.RegisterAsync(dto, cToken);

            //Devolver resultado (201)
            return StatusCode(201, ApiObjResponse<UserDto>.Success(result));
        }
    }
}
