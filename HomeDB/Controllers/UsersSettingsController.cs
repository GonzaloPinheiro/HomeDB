using HomeDB.Application.Authorization.Attributes;
using HomeDB.Application.DTOs;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDB.Controllers
{
    [Route("api")]
    public class UsersSettingsController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly UserSettingsService _userSettingsService;
        private readonly UserAdminSettingsService _userAdminSettingsService;
        private readonly Logger _logger;

        //Constructores
        public UsersSettingsController(UserSettingsService userSettingsService,UserAdminSettingsService userAdminSettingsService,Logger logger)
        {
            _userSettingsService = userSettingsService;
            _userAdminSettingsService = userAdminSettingsService;
            _logger = logger;
        }

        #region Acciones de configuración de usuario para el propio usuario
        /// <summary>
        /// Obtiene la configuración del usuario actual
        /// </summary>
        [HttpGet("users/me/settings")]
        [Authorize]
        public async Task<IActionResult> GetMySettings(CancellationToken cToken)
        {
            //Variables y objetos
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UsersSettingsController",
                operation: $"GetMySettings()",
                correlationId: GetCorrelationId(),
                userId: userId.ToString());

            //Obtiene la configuración del usuario actual
            UserSettingsResponseDto dto = await _userSettingsService.GetSettingsForCurrentUserAsync(cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserSettingsResponseDto>.Success(dto));
        }

        /// <summary>
        /// Actualiza la configuración del usuario actual
        /// </summary>
        [HttpPatch("users/me/settings")]
        [Authorize]
        public async Task<IActionResult> UpdateMySettings([FromBody] UpdateUserSettingsRequestDto dto,CancellationToken cToken)
        {
            //Variables y objetos
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UsersSettingsController",
                operation: $"UpdateMySettings()",
                correlationId: GetCorrelationId(),
                userId: userId.ToString());

            //Actualiza la configuración del usuario actual
            UserSettingsResponseDto result = await _userSettingsService.UpdateSettingsForCurrentUserAsync(dto, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserSettingsResponseDto>.Success(result));
        }

        /// <summary>
        /// Obtiene un resumen del perfil del usuario actual, incluyendo configuración y límites de administración
        /// </summary>
        [HttpGet("users/me/settings-overview")]
        [Authorize]
        public async Task<IActionResult> GetMySettingsProfile(CancellationToken cToken)
        {
            //Variables y objetos
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UsersSettingsController",
                operation: $"GetMySettingsProfile()",
                correlationId: GetCorrelationId(),
                userId: userId.ToString());

            //Obtiene el perfil del usuario actual(configuración y límites de administración)
            UserProfileDto dto = await _userSettingsService.GetProfileForCurrentUserAsync(cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserProfileDto>.Success(dto));
        }
        #endregion

        #region Acciones de configuración de usuario para el administrador
        /// <summary>
        /// Obtiene la configuración de un usuario específico para el administrador
        /// </summary>
        [HttpGet("admin/users/{id:int}/settings")]
        [Authorize]
        [RequireModule(AppModules.UserManagement)]
        public async Task<IActionResult> GetAdminSettings(int id, CancellationToken cToken)
        {
            //Variables y objetos
            int requestingUserId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UsersSettingsController",
                operation: $"GetAdminSettings()",
                correlationId: GetCorrelationId(),
                userId: requestingUserId.ToString());

            //Obtiene la configuración del usuario especificado por el administrador
            UserAdminSettingsResponseDto dto = await _userAdminSettingsService.GetAdminSettingsAsync(id, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserAdminSettingsResponseDto>.Success(dto));
        }

        /// <summary>
        /// Actualiza la configuración de un usuario específico para el administrador
        /// </summary>
        [HttpPatch("admin/users/{id:int}/settings")]
        [Authorize]
        [RequireModule(AppModules.UserManagement)]
        public async Task<IActionResult> UpdateAdminSettings(int id, [FromBody] UpdateUserAdminSettingsRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            int requestingUserId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Api.Controllers.UsersSettingsController",
                operation: $"UpdateAdminSettings()",
                correlationId: GetCorrelationId(),
                userId: requestingUserId.ToString());

            //Actualiza la configuración del usuario especificado por el administrador
            UserAdminSettingsResponseDto result = await _userAdminSettingsService.UpdateAdminSettingsAsync(id, dto, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UserAdminSettingsResponseDto>.Success(result));
        }
        #endregion
    }
}