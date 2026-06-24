using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class AuditLogEntryRepository : IAuditLogRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public AuditLogEntryRepository(AppDbContext context)
        {
            _context = context;
        }

        //Inserta el audoit log entry en la base de datos
        public async Task InsertAsync(AuditLogEntry auditLogEntry, CancellationToken cToken)
        {
            await _context.AuditEntries.AddAsync(auditLogEntry, cToken);
            await _context.SaveChangesAsync(cToken);
        }

        //Obtiene los audit log entries de la base de datos con filtros y paginación
        public async Task<(IEnumerable<AuditLogEntry> Items, int TotalCount)> GetAuditLogsAsync(
                                                  int pageNumber, int pageSize, DateTimeOffset? from, DateTimeOffset? to,
                                                  int? userId, string? username, string? action, 
                                                  string? resourceType, CancellationToken cToken)
        {
            //Construir consulta base
            IQueryable<AuditLogEntry> query = _context.AuditEntries.AsNoTracking();

            //Aplicar filtros según los parámetros proporcionados
            if (from.HasValue)
                query = query.Where(a => a.TimeStamp >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.TimeStamp <= to.Value);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrEmpty(username))
                query = query.Where(a => a.Username.Contains(username));

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrEmpty(resourceType))
                query = query.Where(a => a.ResourceType != null && a.ResourceType.Contains(resourceType));

            //Obtener total count antes de aplicar paginación
            int totalCount = await query.CountAsync(cToken);

            //Aplicar paginación y ordenar por TimeStamp descendente
            IEnumerable<AuditLogEntry> items = await query
                .OrderByDescending(a => a.TimeStamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cToken);

            //Devolver los resultados y el total count
            return (items, totalCount);
        }

        //Persistir los cambios en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}