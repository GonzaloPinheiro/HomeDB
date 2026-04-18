
namespace HomeDB.Domain.Entities
{
    public class LogEntry
    {
        public int id { get; set; }                                          // PK (identity)
        public DateTimeOffset timeStamp { get; set; } = DateTimeOffset.MinValue; // momento del registro (UTC)
        public string level { get; set; } = string.Empty;                   // Info, Warning, Error, Critical
        public string source { get; set; } = string.Empty;                  // "TFCiclo.Api.Controllers.X"
        public string operation { get; set; } = string.Empty;               // Nombre de la operación/método
        public string message { get; set; } = string.Empty;                 // Mensaje principal
        public string exception { get; set; } = string.Empty;               // StackTrace / excepción (nullable)
        public string userId { get; set; } = string.Empty;                  // id del usuario si aplica (nullable)
        public string correlationId { get; set; } = string.Empty;           // id para correlacionar peticiones (GUID)
        public long durationMs { get; set; }                                 // Duración en ms (nullable, si aplica)
        public string metadataJson { get; set; } = string.Empty;            // JSON con datos extra (nullable)
    }
}
