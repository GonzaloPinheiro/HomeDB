using HomeDB.Domain.Common.Enums;

namespace HomeDB.Domain.Entities
{
    public class BackupAuditEntry
    {
        public int Id { get; set; } //PK (identity)
        public BackupLevel Level { get; set; } //Nivel del backup (Daily, Monthly)
        public DateTime StartedAt { get; set; } //Momento de inicio del backup (UTC)
        public DateTime? CompletedAt { get; set; } //Momento de finalización del backup (UTC)
        public BackupStatus Status { get; set; } //Estado del backup (Running, Success, Failed)
        public long FilesBackedUpSizeBytes { get; set; } //Tamaño total de los archivos respaldados en bytes
        public long DatabaseDumpSizeBytes { get; set; } //Tamaño del volcado de la base de datos en bytes
        public string? ErrorMessage { get; set; } //Mensaje de error en caso de fallo 
        public string? BackupPath { get; set; } //Ruta del backup generado
        public DateTime? DeletedAt { get; set; } //Momento de eliminación del backup (UTC)
    }
}