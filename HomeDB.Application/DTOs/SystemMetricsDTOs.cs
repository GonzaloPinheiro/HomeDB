
namespace HomeDB.Application.DTOs
{
    //DTO para la respuesta de las métricas del sistema
    public record SystemMetricsResponseDto(DateTimeOffset Timestamp, double? CpuUsagePercent, long? MemoryTotalBytes, long? MemoryUsedBytes,
                                            double? MemoryUsagePercent, long? DiskTotalBytes, long? DiskUsedBytes, double? DiskUsagePercent, double? TemperatureCelsius);

}