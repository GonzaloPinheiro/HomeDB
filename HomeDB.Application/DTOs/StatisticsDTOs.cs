
namespace HomeDB.Application.DTOs
{
    //DTO para la respuesta de las estadísticas de almacenamiento
    public record StorageStatisticsResponseDto(int totalFiles, int totalFolders, long totalSizeBytes, double totalSizeMb);
}