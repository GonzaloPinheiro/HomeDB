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
    [Route("api/statistics")]
    public class StatisticsController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly Logger _logger;
        private readonly StatisticsService _statisticsService;

        //Constructores
        public StatisticsController(Logger logger, StatisticsService statisticsService)
        {
            _logger = logger;
            _statisticsService = statisticsService;
        }

        [HttpGet]
        [Route("storage")]
        public async Task<IActionResult> GetStorageStatisticsAsync(CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.StatisticsController",
                operation: "GetStorageStatisticsAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener las estadísticas de almacenamiento del usuario
            StorageStatisticsResponseDto result = await _statisticsService.GetUserStorageStatsAsync(userId, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<StorageStatisticsResponseDto>.Success(result));
        }
    }
}
