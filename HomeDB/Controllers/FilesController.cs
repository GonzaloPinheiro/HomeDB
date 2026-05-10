using HomeDB.Application.DTOs.Files;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDB.Controllers
{
    [Route("api/files")]
    public class FilesController : ApiControllerBase
    {
        private readonly Logger _logger;
        private readonly FilesService _filesService;

        public FilesController(Logger logger, FilesService filesService)
        {
            _logger = logger;
            _filesService = filesService;
        }

        [Authorize]
        [HttpPost]
        [Route("uploadFile")]
        public async Task<IActionResult> UploadFileAsync([FromForm] IFormFile file,
                                                         [FromForm] int? folderId,
                                                          CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "UploadFileAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Crear DTO para el servicio
            UploadFileRequestDto dto = new UploadFileRequestDto(
                file.OpenReadStream(),  // Stream sin cargar en memoria
                file.FileName,          // Nombre original
                file.Length,            // Tamaño en bytes
                file.ContentType,       // Tipo MIME
                folderId                // Carpeta destino
            );

            //Subir el archivo
            UploadFileResponseDto result = await _filesService.UploadFileAsync(dto, userId, cToken);

            //Devolver resultado (200)
            return StatusCode(201, ApiObjResponse<UploadFileResponseDto>.Success(result));
        }
    }
}