using HomeDB.Domain.Entities;

namespace HomeDB.Infrastructure.Observability
{
    // Interfaz que expone la operación rápida de encolar logs
    public interface ILogQueue
    {
        // Encola un log de forma rápida; no espera a la persistencia.
        Task EnqueueAsync(LogEntry entry);
    }
}
