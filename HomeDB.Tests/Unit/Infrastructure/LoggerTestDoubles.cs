using HomeDB.Application.Options;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Observability;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Dobles de prueba para poder usar el Logger real (clase concreta, sin interfaz) y capturar lo que registra.
    internal sealed class FakeLogQueue : ILogQueue
    {
        public List<LogEntry> Entries { get; } = new List<LogEntry>();

        public Task EnqueueAsync(LogEntry entry)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Crea un Logger real cuya cola es esta. El sumidero de fallos escribe bajo failedLogsPath, que debe ser temporal.
        /// </summary>
        public Logger CreateLogger(string failedLogsPath)
        {
            LogFailureFileSink sink = new LogFailureFileSink(Options.Create(new LogFallbackOptions { BasePath = failedLogsPath }));
            return new Logger(new FakeLogEntryRepository(), this, sink);
        }
    }

    //El Logger exige un repositorio en el constructor pero AddAsync no lo usa
    internal sealed class FakeLogEntryRepository : ILogEntryRepository
    {
        public Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken) => throw new NotSupportedException();

        public Task<(IEnumerable<LogEntry> Items, int TotalCount)> GetLogsAsync(string? level, string? operation,
            DateTimeOffset? from, DateTimeOffset? to, string? correlationId, int page, int pageSize, CancellationToken cToken)
            => throw new NotSupportedException();

        public Task<IEnumerable<(string Level, DateTimeOffset TimeStamp)>> GetHealthAsync(CancellationToken cToken)
            => throw new NotSupportedException();

        public Task<IEnumerable<(string Operation, int Count)>> GetErrorSummaryAsync(int hours, CancellationToken cToken)
            => throw new NotSupportedException();

        public Task<IEnumerable<(string Operation, long DurationMs, DateTimeOffset TimeStamp)>> GetSlowOperationsAsync(long thresholdMs, CancellationToken cToken)
            => throw new NotSupportedException();
    }
}
