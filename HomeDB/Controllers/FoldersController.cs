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
    [Authorize]
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

        [HttpPost]
        [Route("create")]
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


        [HttpGet]
        [Route("{folderId?}")]
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