using HomeDB.Application.DTOs.Auth;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        /// </summary>s
        [EnableRateLimiting(nameof(RateLimiterNames.Auth))]
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync(RegisterDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string ipAddress = GetIpAddress();
            string username = User.Identity?.Name ?? "Unknown";

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "RegisterAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            UserDto result = await _authService.RegisterAsync(dto, ipAddress, cToken);

            //Devolver resultado (201)
            return StatusCode(201, ApiObjResponse<UserDto>.Success(result));
        }

        /// <summary>
        /// Autentica un usuario y devuelve un access token y un refresh token
        /// </summary>
        [EnableRateLimiting(nameof(RateLimiterNames.Auth))]
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> LoginAsync(LoginDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string ipAddress = GetIpAddress();
            string username = User.Identity?.Name ?? "Unknown";

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "LoginAsync()",
                correlationId: correlationId,
                userId: username);

            //Registrar el usuario
            TokenResponseDto result = await _authService.LoginAsync(dto, ipAddress, cToken);

            //Adjuntar tokens en cookies seguras
            AppendAuthCookies(result);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<TokenResponseDto>.Success(result));
        }

        /// <summary>
        /// Renueva el access token usando un refresh token válido.
        /// </summary>
        [EnableRateLimiting(nameof(RateLimiterNames.Auth))]
        [HttpPost]
        [Route("refreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown";
            string? refreshTokenFromCookie = Request.Cookies[nameof(CookieNames.RefreshToken)];

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "RefreshTokenAsync()",
                correlationId: correlationId,
                userId: username);

            //Intentar obtener el refresh token de la cookie si no se proporcionó en el cuerpo de la solicitud
            if (!string.IsNullOrEmpty(refreshTokenFromCookie))
            {
                dto = dto with { RefreshToken = refreshTokenFromCookie };
            }

            //Registrar el usuario
            TokenResponseDto result = await _authService.RefreshAsync(dto, cToken);

            //Rotas las cookies con los nuevos tokens
            AppendAuthCookies(result);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<TokenResponseDto>.Success(result));
        }

        /// <summary>
        /// Invalida el refresh token del usuario cerrando la sesión.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("logout")]
        public async Task<IActionResult> LogoutAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            string username = User.Identity?.Name ?? "Unknown";
            string? refreshTokenFromCookie = Request.Cookies[nameof(CookieNames.RefreshToken)];

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuthController",
                operation: "LogoutAsync()",
                correlationId: correlationId,
                userId: username);

            //Intentar obtener el refresh token de la cookie si no se proporcionó en el cuerpo de la solicitud
            if (!string.IsNullOrEmpty(refreshTokenFromCookie))
            {
                dto = dto with { RefreshToken = refreshTokenFromCookie };
            }

            //Registrar el usuario
            await _authService.LogoutAsync(dto, cToken);

            //Borrar cookies de autenticación
            DeleteAuthCookies();

            //Devolver resultado vacío siempre
            return Ok(ApiObjResponse<object>.Success(null));
        }

        #region Funciones privadas
        private const string RefreshTokenCookiePath = "/api/auth";

        /// <summary>
        /// Agrega las cookies de autenticación (access token y refresh token) a la respuesta.
        /// </summary>
        private void AppendAuthCookies(TokenResponseDto tokens)
        {
            //Configuración de cookies
            CookieOptions accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,       //No accesible desde JavaScript
                Secure = true,         //Solo se envía por HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30) //Mismo TTL que el token JWT
            };

            CookieOptions refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = RefreshTokenCookiePath,
                Expires = DateTimeOffset.UtcNow.AddDays(7) //Mismo TTL que el refresh token
            };

            Response.Cookies.Append(nameof(CookieNames.AccessToken), tokens.AccessToken, accessTokenOptions);
            Response.Cookies.Append(nameof(CookieNames.RefreshToken), tokens.RefreshToken, refreshTokenOptions);
        }

        /// <summary>
        /// Elimina las cookies de autenticación de la respuesta, cerrando la sesión del usuario.
        /// </summary>
        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete(nameof(CookieNames.AccessToken));
            Response.Cookies.Delete(nameof(CookieNames.RefreshToken), new CookieOptions
            {
                Path = RefreshTokenCookiePath
            });
        }
        #endregion
    }
}