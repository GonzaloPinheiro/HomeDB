using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class LogEntryRepository : ILogEntryRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public LogEntryRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            try
            {
                await using AppDbContext context = await _contextFactory.CreateDbContextAsync(cToken);
                await context.Logs.AddAsync(log, cToken);
                await context.SaveChangesAsync(cToken);
                return log.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar log: {ex}");
                return 0;
            }
        }
    }
}
