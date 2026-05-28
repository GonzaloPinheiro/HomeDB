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

        //Inserción de logs
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

        //Lectura de logs con filtros y paginación
        public async Task<(IEnumerable<LogEntry> Items, int TotalCount)> GetLogsAsync(string? level, string? operation, DateTimeOffset? fromPage, DateTimeOffset? toPage,
                                                                                      string? correlationId, int page, int pageSize, CancellationToken cToken)
        {
            //Crear contexto
            await using AppDbContext context = await _contextFactory.CreateDbContextAsync(cToken);

            //Construir consulta base
            IQueryable<LogEntry> query = context.Logs.AsNoTracking();

            //Aplicar filtros
            if (!string.IsNullOrWhiteSpace(level))
                query = query.Where(l => l.Level == level);

            if (!string.IsNullOrWhiteSpace(operation))
                query = query.Where(l => l.Operation.Contains(operation));

            if (fromPage.HasValue)
                query = query.Where(l => l.TimeStamp >= fromPage.Value);

            if (toPage.HasValue)
                query = query.Where(l => l.TimeStamp <= toPage.Value);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(l => l.CorrelationId == correlationId);

            //Obtener total count antes de aplicar paginación
            int totalCount = await query.CountAsync(cToken);

            //Aplicar orden, paginación y ejecutar consulta
            IEnumerable<LogEntry> items = await query
                .OrderByDescending(l => l.TimeStamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cToken);

            return (items, totalCount);
        }


        //Obtener métricas de salud: número de errores y warnings en la última hora y últimas 24 horas
        public async Task<IEnumerable<(string Level, DateTimeOffset TimeStamp)>> GetHealthAsync(
            CancellationToken cToken)
        {
            //Crear contexto
            await using AppDbContext context = await _contextFactory.CreateDbContextAsync(cToken);

            //Rangos de tiempo
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset oneHourAgo = now.AddHours(-1);
            DateTimeOffset oneDayAgo = now.AddHours(-24);

            //Trae solo Level y TimeStamp, nada más
            List<(string Level, DateTimeOffset TimeStamp)> relevant = await context.Logs
                .AsNoTracking()
                .Where(l => l.TimeStamp >= oneDayAgo && (l.Level == "Error" || l.Level == "Warning"))
                .Select(l => new { l.Level, l.TimeStamp })
                .ToListAsync(cToken)
                .ContinueWith(t => t.Result.Select(x => (x.Level, x.TimeStamp)).ToList(), cToken);

            //Devolver logs resultantes
            return relevant;
        }

        //Obtener resumen de errores por operación en las últimas N horas.
        public async Task<IEnumerable<(string Operation, int Count)>> GetErrorSummaryAsync(int hours, CancellationToken cToken)
        {
            //Crear contexto
            await using AppDbContext context = await _contextFactory.CreateDbContextAsync(cToken);

            //Rango de tiempo
            DateTimeOffset from = DateTimeOffset.UtcNow.AddHours(-hours);

            //Agrupar por operación y contar errores, ordenando de mayor a menor
            IEnumerable<(string Operation, int Count)> results = await context.Logs
                .AsNoTracking()
                .Where(l => l.Level == "Error" && l.TimeStamp >= from)
                .GroupBy(l => l.Operation)
                .Select(g => new { Operation = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cToken)
                .ContinueWith(t => t.Result.Select(x => (x.Operation, x.Count)), cToken);

            //Devolver resultados
            return results;
        }

        public async Task<IEnumerable<(string Operation, long DurationMs, DateTimeOffset TimeStamp)>> GetSlowOperationsAsync(long thresholdMs, CancellationToken cToken)
        {
            //Crear contexto
            await using AppDbContext context = await _contextFactory.CreateDbContextAsync(cToken);

            //Filtrar logs por duración, ordenarlos de mayor a menor y traer solo los campos necesarios
            IEnumerable<(string, long, DateTimeOffset)> results = await context.Logs
                .AsNoTracking()
                .Where(l => l.DurationMs > thresholdMs)
                .OrderByDescending(l => l.DurationMs)
                .Take(50)
                .Select(l => new { l.Operation, l.DurationMs, l.TimeStamp })
                .ToListAsync(cToken)
                .ContinueWith(t => t.Result.Select(x => (x.Operation, x.DurationMs, x.TimeStamp)), cToken);

            //Devolver resultados
            return results;
        }
    }
}