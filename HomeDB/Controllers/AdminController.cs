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
    [Authorize(Roles = nameof(RolesList.Admin))]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    [Route("api/admin/logs")]
    public class AdminController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly LogsService _logQueryService;
        private readonly Logger _logger;

        //Constuctores
        public AdminController(LogsService logQueryService, Logger logger)
        {
            _logQueryService = logQueryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogsAsync([FromQuery] GetLogsRequestDto query, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AdminController",
                operation: "GetLogsAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener los logs usando el servicio
            GetLogsResponseDto result = await _logQueryService.GetLogsAsync(query, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<GetLogsResponseDto>.Success(result));
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealthAsync(CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AdminController",
                operation: "GetHealthAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el estado de salud de los logs usando el servicio
            LogHealthResponseDto result = await _logQueryService.GetHealthAsync(cToken);

            //Todo Ok
            return Ok(ApiObjResponse<LogHealthResponseDto>.Success(result));
        }

        [HttpGet("error-summary")]
        public async Task<IActionResult> GetErrorSummaryAsync([FromQuery] int hours = 24, CancellationToken cToken = default)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AdminController",
                operation: "GetErrorSummaryAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el resumen de errores usando el servicio
            IEnumerable<LogErrorSummaryItemDto> result =
                await _logQueryService.GetErrorSummaryAsync(hours, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<IEnumerable<LogErrorSummaryItemDto>>.Success(result));
        }

        [HttpGet("slow-operations")]
        public async Task<IActionResult> GetSlowOperationsAsync([FromQuery] long thresholdMs = 2000, CancellationToken cToken = default)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AdminController",
                operation: "GetSlowOperationsAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener las operaciones lentas usando el servicio
            IEnumerable<LogSlowOperationDto> result =
                await _logQueryService.GetSlowOperationsAsync(thresholdMs, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<IEnumerable<LogSlowOperationDto>>.Success(result));
        }
    }
}