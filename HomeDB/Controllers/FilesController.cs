using HomeDB.Application.Authorization.Attributes;
using HomeDB.Application.DTOs.Files;
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
    [Route("api/files")]
    [Authorize]
    [RequireModule(AppModules.Files)]
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

        /// <summary>
        /// Lista los archivos de una carpeta específica o de la raíz si no se proporciona folderId.
        /// </summary>
        [HttpGet]
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

        /// <summary>
        /// Sube un archivo al servidor. El archivo se envía como multipart/form-data y se puede especificar una carpeta de destino mediante folderId.
        /// </summary>
        [HttpPost]
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

            //Devolver resultado
            return StatusCode(201, ApiObjResponse<UploadFileResponseDto>.Success(result));
        }

        /// <summary>
        /// Descarga un archivo específico por su ID. Devuelve el archivo como una respuesta física con el tipo de contenido y nombre de archivo adecuados.
        /// </summary>
        [HttpGet]
        [Route("{id:int}")]
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

        /// <summary>
        /// Hace una búsqueda entre todos los archivos para los parámetros recibidos
        /// </summary>
        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchFileAsync([FromQuery]SearchFileRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "SearchFileAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener lo encontrado
            SearchFilesResponseDto result = await _filesService.SearchFileAsync(userId, dto, cToken);

            //Devolver los arvhivos encontrados
            return StatusCode(200, ApiObjResponse<SearchFilesResponseDto>.Success(result));
        }

        /// <summary>
        /// Actualiza los datos de un archivo específico por su ID. Devuelve un UpdateFileResponseDto con los detalles.
        /// </summary>
        [HttpPatch]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateFileAsync(int id, [FromBody] UpdateFileRequestDto dto, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.FilesController",
                operation: "UpdateFileAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Actualizar el archivo
            UpdateFileResponseDto result = await _filesService.UpdateFileAsync(id, dto, userId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<UpdateFileResponseDto>.Success(result));
        }

        /// <summary>
        /// Elimina un archivo específico por su ID. Devuelve un objeto de respuesta que indica si la eliminación fue exitosa.
        /// </summary>
        [HttpDelete]
        [Route("{id:int}")]
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