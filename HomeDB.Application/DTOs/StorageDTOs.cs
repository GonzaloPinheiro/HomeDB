using HomeDB.Domain.Common;

namespace HomeDB.Application.DTOs
{
    //DTO con el estado de una ubicación de almacenamiento. Los datos de disponibilidad y capacidad son los del disco
    //que la respalda, así que se repiten en las ubicaciones que comparten disco. Si el disco no está disponible,
    //TotalBytes, UsedBytes y FreeBytes son null. CheckedAtUtc es el momento de la última comprobación completa.
    public record StorageLocationStatusDto(string LocationKey, string DiskKey, LocationUsage Usage, bool IsDefault,
                                           bool IsAvailable, bool IsWritable, string? Reason, DateTime CheckedAtUtc,
                                           long? TotalBytes, long? UsedBytes, long? FreeBytes,
                                           long MinFreeBytes, long? MaxUsedBytes);
}
