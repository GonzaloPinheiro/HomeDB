using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class BackupAuditRepository : IBackupAuditRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public BackupAuditRepository(AppDbContext context)
        {
            _context = context;
        }

        //Devuelve la entrada de auditoría más antigua que esté activa (no eliminada) para un nivel de respaldo específico
        public async Task<BackupAuditEntry?> GetOldestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true)
        {
            // Crea una consulta para obtener las entradas de auditoría de respaldo
            IQueryable<BackupAuditEntry> query = _context.BackupAuditEntries;

            // Filtra la consulta para obtener solo las entradas que coincidan con el nivel de respaldo especificado y que no estén eliminadas
            query = query.Where(entry => entry.Level == level && entry.DeletedAt == null);

            // Si se especifica, aplica el seguimiento de cambios desactivado a la consulta
            if (asNoTracking)
                query = query.AsNoTracking();

            // Ordena la consulta por la fecha de inicio y devuelve la primera entrada encontrada o null si no hay ninguna
            return await query
                .OrderBy(entry => entry.StartedAt)
                .FirstOrDefaultAsync(cToken);
        }

        //Agrega una nueva entrada de auditoría a la base de datos
        public async Task AddAsync(BackupAuditEntry entry, CancellationToken cToken)
        {
            await _context.BackupAuditEntries.AddAsync(entry, cToken);
        }

        //Persiste los cambios realizados en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}