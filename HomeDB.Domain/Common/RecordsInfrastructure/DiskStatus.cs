namespace HomeDB.Domain.Common.RecordsInfrastructure
{
    /// <summary>
    /// Estado de disponibilidad de un disco de almacenamiento en un instante dado.
    /// Reason es un texto genérico y nunca incluye rutas del host, porque puede acabar en la respuesta de la API.
    /// </summary>
    public record DiskStatus(string DiskKey, bool IsAvailable, bool IsWritable, string? Reason, DateTime CheckedAtUtc);
}
