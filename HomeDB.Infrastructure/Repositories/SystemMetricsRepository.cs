using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class SystemMetricsRepository : ISystemMetricsRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public SystemMetricsRepository(AppDbContext context)
        {
            _context = context;
        }


        //Inserta una metrica del sistema
        public async Task InsertAsync(SystemMetricsEntry entry, CancellationToken cToken)
        {
            await _context.SystemMetricsEntries.AddAsync(entry, cToken);
            await _context.SaveChangesAsync(cToken);
        }

        //Devuelve una lista de entradas en base al parámetro from recibido
        public async Task<IEnumerable<SystemMetricsEntry>> GetRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cToken)
        {
            return await _context.SystemMetricsEntries
                .AsNoTracking()
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderBy(e => e.Timestamp)
                .ToListAsync(cToken);
        }

        //Devuelve el registro de métricas más reciente
        public async Task<SystemMetricsEntry?> GetLastAsync(CancellationToken cToken)
        {
            return await _context.SystemMetricsEntries
                .AsNoTracking()
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync(cToken);
        }

        //Borra los registros a partir del cutoff recibido
        public async Task DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cToken)
        {
            await _context.SystemMetricsEntries
                .Where(e => e.Timestamp < cutoff)
                .ExecuteDeleteAsync(cToken);
        }
    }
}