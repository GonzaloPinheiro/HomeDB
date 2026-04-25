
namespace HomeDB.Domain.Entities
{
    public class LogEntry
    {
        public int Id { get; set; }                                          // PK (identity)
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.MinValue; // momento del registro (UTC)
        public string Level { get; set; } = string.Empty;                   // Info, Warning, Error, Critical
        public string Source { get; set; } = string.Empty;                  // "TFCiclo.Api.Controllers.X"
        public string Operation { get; set; } = string.Empty;               // Nombre de la operación/método
        public string Message { get; set; } = string.Empty;                 // Mensaje principal
        public string Exception { get; set; } = string.Empty;               // StackTrace / excepción (nullable)
        public string UserId { get; set; } = string.Empty;                  // id del usuario si aplica (nullable)
        public string CorrelationId { get; set; } = string.Empty;           // id para correlacionar peticiones (GUID)
        public long DurationMs { get; set; }                                 // Duración en ms (nullable, si aplica)
        public string MetadataJson { get; set; } = string.Empty;            // JSON con datos extra (nullable)
    }
}
