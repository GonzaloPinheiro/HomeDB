using HomeDB.Application.DTOs.Auth;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDB.Controllers
{
    
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

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Autentica un usuario y devuelve un access token y un refresh token
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> LoginAsync(LoginDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown"; // username del JWT

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "LoginAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            TokenResponseDto result = await _authService.LoginAsync(dto, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<TokenResponseDto>.Success(result));
        }


        /// <summary>
        /// Renueva el access token usando un refresh token válido.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("refreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown"; // username del JWT

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "RefreshTokenAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            TokenResponseDto result = await _authService.RefreshAsync(dto, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<TokenResponseDto>.Success(result));
        }

        /// <summary>
        /// Invalida el refresh token del usuario cerrando la sesión.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [Route("logout")]
        public async Task<IActionResult> LogoutAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown"; // username del JWT

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "LogoutAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            await _authService.LogoutAsync(dto, cToken);

            //Devolver resultado vacío siempre
            return Ok(ApiObjResponse<object>.Success(null));
        }
    }
}