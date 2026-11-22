using HomeDB.Application.DTOs;
using HomeDB.Application.Services;
using HomeDB.Authorization.Attributes;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Route("api/storage")]
    [Authorize]
    [RequireModule(AppModules.SystemMonitor)]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    public class StorageController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly StorageStatusService _storageStatusService;
        private readonly Logger _logger;

        //Constructor
        public StorageController(StorageStatusService storageStatusService, Logger logger)
        {
            _storageStatusService = storageStatusService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el estado de cada ubicación de almacenamiento: disponibilidad, escritura, capacidad y límites.
        /// Responde 200 aunque haya discos caídos: ese estado es justo lo que se consulta.
        /// </summary>
        [HttpGet("locations")]
        public async Task<IActionResult> GetLocationsStatusAsync()
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.StorageController",
                operation: "GetLocationsStatusAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtiene el estado de las ubicaciones
            IReadOnlyList<StorageLocationStatusDto> locations = _storageStatusService.GetLocationsStatus();

            //Devolver resultado
            return Ok(ApiObjResponse<IReadOnlyList<StorageLocationStatusDto>>.Success(locations));
        }
    }
}
