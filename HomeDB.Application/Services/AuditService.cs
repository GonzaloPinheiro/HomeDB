using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class AuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public AuditService(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository;
            _currentUserService = currentUserService;
        }

        //Loguea en el audit log una acción genérica
        public async Task LogAsync(string action,
                                   string? resourceType = null,
                                   int? resourceId = null,
                                   string? resourceName = null,
                                   CancellationToken cToken = default)
        {
            AuditLogEntry entry = new AuditLogEntry
            {
                UserId = _currentUserService.UserId,
                Username = _currentUserService.Username,
                IpAddress = _currentUserService.IpAddress,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                ResourceName = resourceName
            };

            await _auditLogRepository.InsertAsync(entry, cToken);

        }

        //Audit log específico para auth actions
        public async Task LogAuthAsync(int userId, string username, string? ipAddress, string action, CancellationToken cToken)
        {
            AuditLogEntry entry = new AuditLogEntry
            {
                UserId = userId,
                Username = username,
                IpAddress = ipAddress,
                Action = action
            };

            await _auditLogRepository.InsertAsync(entry, cToken);

        }
    }
}