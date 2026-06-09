using HomeDB.Application.DTOs.Files;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Route("api/files")]
    [Authorize]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    public class FilesController : ApiControllerBase
    {
        private readonly Logger _logger;
        private readonly FilesService _filesService;

        public FilesController(Logger logger, FilesService filesService)
        {
            _logger = logger;
            _filesService = filesService;
        }

        [HttpGet]
        [Route("listFiles")]
        public async Task<IActionResult> ListFilesAsync([FromQuery] int? folderId, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "ListFilesAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener la lista de archivos de la carpeta (o raíz si folderId es null)
            IEnumerable<GetFileItemDto> files = await _filesService.ListFilesAsync(userId, folderId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<IEnumerable<GetFileItemDto>>.Success(files));
        }

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

        [HttpGet]
        [Route("{id}/downloadFile")]
        public async Task<IActionResult> DownloadFileAsync(int id, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "DownloadFileAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener los datos del archivo para servirlo
            DownloadFileResponseDto result = await _filesService.DownloadFileAsync(id, userId, cToken);

            //Devolver el archivo (200)
            return PhysicalFile(result.FilePath, result.ContentType, result.FileName);
        }

        [HttpDelete]
        [Route("{id}/deleteFile")]
        public async Task<IActionResult> DeleteFileAsync(int id, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "DeleteFileAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Eliminar el archivo
            DeleteFileResponseDto result = await _filesService.DeleteFileAsync(id, userId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<DeleteFileResponseDto>.Success(result));
        }
    }
}