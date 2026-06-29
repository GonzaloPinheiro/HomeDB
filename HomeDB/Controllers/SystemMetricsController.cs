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
    [Route("api/system-metrics")]
    [Authorize(Roles = nameof(RolesList.Admin))]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    public class SystemMetricsController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly SystemMetricsService _systemMetricsService;
        private readonly Logger _logger;

        //Constructor
        public SystemMetricsController(SystemMetricsService systemMetricsService, Logger logger)
        {
            _systemMetricsService = systemMetricsService;
            _logger = logger;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cToken) //TODO Añadir dto entrada con to máximo de 30 días(tiempo que se guardan las métricas)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.SystemMetricsController",
                operation: "GetHistoryAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtiene el historial de las métricas del sistema
            IEnumerable<SystemMetricsResponseDto> history = await _systemMetricsService.GetHistoryAsync(from, to, cToken);

            //Devolver resultado
            return Ok(ApiObjResponse<IEnumerable<SystemMetricsResponseDto>>.Success(history));
        }

        [HttpGet("last-metric")]
        public async Task<IActionResult> GetLastMetricAsync(CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.SystemMetricsController",
                operation: "GetLastMetricAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtiene la últimamétrica del sistema
            SystemMetricsResponseDto lastMetric = await _systemMetricsService.GetLastMetricAsync(cToken);

            //Devolver resultado
            return Ok(ApiObjResponse<SystemMetricsResponseDto>.Success(lastMetric));
        }
    }
}