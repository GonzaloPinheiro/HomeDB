using HomeDB.Authorization.Attributes;
using HomeDB.Application.DTOs;
using HomeDB.Application.Services;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Route("api/backup")]
    [Authorize(Roles = nameof(RolesList.Admin))]
    [RequireModule(AppModules.SystemMonitor)]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    public class BackupController : ApiControllerBase
    {
        //Variables y objetos globales
        private readonly Logger _logger;
        private readonly BackupService _backupService;

        //Constructores
        public BackupController(Logger logger, BackupService backupService)
        {
            _logger = logger;
            _backupService = backupService;
        }

        [HttpGet]
        [Route("history")]
        public async Task<IActionResult> BackupsHistory([FromQuery] GetBackupHistoryRequestDto query, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.BackupController",
                operation: "BackupsHistory()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Obtener el historial de backups usando el servicio
            GetBackupHistoryResponseDto result = await _backupService.GetHistoryAsync(query, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<GetBackupHistoryResponseDto>.Success(result));
        }

        [HttpPost]
        [Route("{level}/trigger")]
        public async Task<IActionResult> TriggerBackup([FromRoute] BackupLevel level, CancellationToken cToken)
        {
            //Variables y objetos
            string correlationId = GetCorrelationId();
            int userId = GetUserId();

            //Comienza scope: registra entrada automáticamente y registrará salida al finalizar using.
            await using OperationLogScope scope = _logger.BeginScope(
                source: "HomeDB.Controllers.BackupController",
                operation: "TriggerBackup()",
                correlationId: correlationId,
                userId: userId.ToString());

            //Lanzar manualmente el backup para el nivel indicado y esperar a que termine
            BackupAuditEntry result = await _backupService.TriggerBackupAsync(level, cToken);

            //Devolver resultado (200)
            return Ok(ApiObjResponse<BackupAuditEntry>.Success(result));
        }
    }
}