using HomeDB.Application.Authorization.Attributes;
using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Interfaces;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Route("api/files/upload")]
    [Authorize]
    [RequireModule(AppModules.Files)]
    [EnableRateLimiting(RateLimiterNames.Global)]
    public class UploadController : ApiControllerBase
    {
        private readonly UploadService _uploadService;
        private readonly Logger _logger;
        private readonly ICurrentUserService _currentUserService;

        public UploadController(UploadService uploadService, Logger logger, ICurrentUserService currentUserService)
        {
            _uploadService = uploadService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        [HttpPost("init")]
        public async Task<IActionResult> InitUploadSessionAsync([FromBody] UploadInitRequestDto request, CancellationToken cToken)
        {

            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UploadController",
                operation: "InitUploadSessionAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Crear la nueva sesión de carga
            UploadInitResponseDto result = await _uploadService.InitUploadAsync(request, userId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<UploadInitResponseDto>.Success(result));
        }

        [HttpPost("chunk")]
        [RequestSizeLimit(35_000_000)] //Margen sobre chunks de 20-30MB
        public async Task<IActionResult> ReceiveChunkAsync([FromForm] Guid sessionId,
                                                            [FromForm] int chunkNumber,
                                                            [FromForm] IFormFile chunk,
                                                             CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UploadController",
                operation: "ReceiveChunkAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Crear DTO para el servicio
            UploadChunkRequestDto request = new UploadChunkRequestDto
            {
                SessionId = sessionId,
                ChunkNumber = chunkNumber,
                ChunkStream = chunk.OpenReadStream() //Stream sin cargar en memoria
            };

            //Recibir el chunk y registrarlo en DB
            await _uploadService.ReceiveChunkAsync(request, _currentUserService.UserId, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<object>.Success("Chunk recibido correctamente."));
        }

        [HttpGet("{sessionId}/status")]
        public async Task<IActionResult> GetUploadStatusAsync(Guid sessionId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UploadController",
                operation: "GetUploadStatusAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el estado de la sesión de carga
            UploadStatusResponseDto response = await _uploadService.GetUploadStatusAsync(sessionId, _currentUserService.UserId, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<UploadStatusResponseDto>.Success(response));
        }

        [HttpPost("{sessionId}/complete")]
        public async Task<IActionResult> CompleteUploadAsync(Guid sessionId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.UploadController",
                operation: "CompleteUploadAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Completar la sesión de carga y obtener el resultado
            UploadFileResponseDto? response = await _uploadService.CompleteUploadAsync(sessionId, userId, cToken);

            //Si la respuesta es null, significa que la subida ya se había completado anteriormente
            if (response is null)
                return Ok(ApiObjResponse<string>.Success("La subida ya se había completado anteriormente."));

            //Todo Ok
            return Ok(ApiObjResponse<UploadFileResponseDto>.Success(response));
        }
    }
}