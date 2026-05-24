using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;

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
        }

        //Persistir los cambios en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}