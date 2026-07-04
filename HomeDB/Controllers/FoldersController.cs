using HomeDB.Application.Authorization.Attributes;
using HomeDB.Application.DTOs;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    [Authorize]
    [RequireModule(AppModules.Files)]
    [Route("api/folders")]
    public class FoldersController : ApiControllerBase
    {
        private readonly Logger _logger;
        private readonly FoldersService _foldersService;

        public FoldersController(Logger logger, FoldersService foldersService)
        {
            _logger = logger;
            _foldersService = foldersService;
        }

        /// <summary>
        /// Crea un nuevo folder para el usuario.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFolderAsync(CreateFolderRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();


            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FoldersController",
                operation: "CreateFolderAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Crea el folder solicitado.
            CreateFolderResponseDto result = await _foldersService.CreateFolderAsync(dto, userId, cToken);

            //Todo Ok
            return StatusCode(201, ApiObjResponse<CreateFolderResponseDto>.Success(result));

        }

        /// <summary>
        /// Obtiene los folders del usuario. Si se proporciona folderId, obtiene solo ese folder; de lo contrario, obtiene los de la raíz.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFolderAsync([FromQuery] int? folderId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();


            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FoldersController",
                operation: "GetFolderAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el foler solicitado.
            IEnumerable<GetFolderResponseDto> result = await _foldersService.GetFoldersAsync(userId, folderId, cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<IEnumerable<GetFolderResponseDto>>.Success(result));
        }

        /// <summary>
        /// Actualiza un folder existente. Se puede cambiar el nombre y/o el folder padre.
        /// </summary>
        [HttpPatch]
        [Route("{folderId}")]
        public async Task<IActionResult> UpdateFolderAsync([FromRoute] int folderId, [FromBody] UpdateFolderRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();


            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FoldersController",
                operation: "UpdateFolderAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Actualizar el folder
            GetFolderResponseDto result = await _foldersService.UpdateFolderAsync(folderId, userId, dto, cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<GetFolderResponseDto>.Success(result));
        }


        /// <summary>
        /// Elimina un folder existente. Solo se puede eliminar si está vacío (sin subfolders ni archivos).
        /// </summary>
        [HttpDelete]
        [Route("{folderId}")]
        public async Task<IActionResult> DeleteFolderAsync([FromRoute] int folderId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FoldersController",
                operation: "DeleteFolderAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Borrar el folder solicitado.
            DeleteFolderResponseDto result = await _foldersService.DeleteFolderAsync(folderId, userId, cToken);

            //Todo Ok
            return StatusCode(200, ApiObjResponse<DeleteFolderResponseDto>.Success(result));
        }
    }
}