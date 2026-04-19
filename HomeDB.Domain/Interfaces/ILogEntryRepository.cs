using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces
{
    public interface ILogEntryRepository
    {
        Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken);
    }
}
