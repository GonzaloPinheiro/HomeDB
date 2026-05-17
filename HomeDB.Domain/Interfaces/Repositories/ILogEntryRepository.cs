using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface ILogEntryRepository
    {
        Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken);
    }
}
