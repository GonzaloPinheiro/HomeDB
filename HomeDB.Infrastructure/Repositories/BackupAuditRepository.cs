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

        //Devuelve la fecha y hora del último backup exitoso completado para un nivel de respaldo específico
        public async Task<DateTime?> GetLastSuccessfulCompletedAtAsync(BackupLevel level, CancellationToken cToken)
        {
            return await _context.BackupAuditEntries
                .Where(entry => entry.Level == level && entry.Status == BackupStatus.Success)
                .OrderByDescending(entry => entry.CompletedAt)
                .Select(entry => entry.CompletedAt)
                .FirstOrDefaultAsync(cToken);
        }

        //Devuelve la entrada de auditoría activa (no eliminada) más reciente para un nivel de respaldo específico
        public async Task<BackupAuditEntry?> GetLatestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true)
        {
            // Crea una consulta para obtener las entradas de auditoría de respaldo
            IQueryable<BackupAuditEntry> query = _context.BackupAuditEntries;

            // Filtra la consulta para obtener solo las entradas que coincidan con el nivel de respaldo especificado y que no estén eliminadas
            query = query.Where(entry => entry.Level == level && entry.DeletedAt == null);

            // Si se especifica, aplica el seguimiento de cambios desactivado a la consulta
            if (asNoTracking)
                query = query.AsNoTracking();

            // Ordena la consulta por la fecha de inicio descendente y devuelve la primera entrada encontrada o null si no hay ninguna
            return await query
                .OrderByDescending(entry => entry.StartedAt)
                .FirstOrDefaultAsync(cToken);
        }

        //Devuelve un listado paginado de los registros de auditoría de backup, opcionalmente filtrado por nivel
        public async Task<(IEnumerable<BackupAuditEntry> Items, int TotalCount)> GetHistoryAsync(BackupLevel? level, int pageNumber, int pageSize, CancellationToken cToken)
        {
            // Crea una consulta para obtener las entradas de auditoría de respaldo, incluyendo las ya eliminadas ya que forman parte del historial
            IQueryable<BackupAuditEntry> query = _context.BackupAuditEntries.AsNoTracking();

            // Si se especifica un nivel, filtra la consulta para obtener solo las entradas de ese nivel
            if (level.HasValue)
                query = query.Where(entry => entry.Level == level.Value);

            // Cuenta el total de entradas que coinciden con el filtro antes de paginar
            int totalCount = await query.CountAsync(cToken);

            // Obtiene la página solicitada, ordenada por fecha de inicio descendente (más recientes primero)
            List<BackupAuditEntry> items = await query
                .OrderByDescending(entry => entry.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cToken);

            return (items, totalCount);
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