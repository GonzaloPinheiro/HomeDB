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
    [Route("api/admin")]
    public class AuditController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly AuditService _auditService;
        private readonly Logger _logger;

        //Constuctores
        public AuditController(AuditService auditService, Logger logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        [Route("auditLogs")]
        public async Task<IActionResult> GetAuditLogsAsync([FromQuery] GetAuditLogsRequestDto query, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.AuditController",
                operation: "GetAuditLogsAsync()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener los logs usando el servicio
            GetAuditLogsResponseDto result = await _auditService.GetAuditLogsAsync(query, cToken);

            //Todo Ok
            return Ok(ApiObjResponse<GetAuditLogsResponseDto>.Success(result));
        }
    }
}