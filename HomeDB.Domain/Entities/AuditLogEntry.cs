
namespace HomeDB.Domain.Entities
{
    public class AuditLogEntry
    {
        public int Id { get; set; } //PK (identity)
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow; //Momento del registro (UTC)
        public int UserId { get; set; } //Id del usuario
        public string Username { get; set; } = string.Empty; //Username del usuario
        public string? IpAddress { get; set; } //Dirección IP del cliente
        public string Action { get; set; } = string.Empty; //Acción realizada
        public string? ResourceType { get; set; } //Tipo de recurso afectado
        public int? ResourceId { get; set; } //Id del recurso afectado
        public string? ResourceName { get; set; } //Nombre del recurso afectado
    }
}