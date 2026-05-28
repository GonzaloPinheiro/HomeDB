using HomeDB.Application.DTOs;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class StatisticsService
    {
        //Variables y objetos globales
        private readonly IFileItemRepository _fileItemRepository;

        //Constructores
        public StatisticsService(IFileItemRepository fileItemRepository)
        {
            _fileItemRepository = fileItemRepository;
        }



        //Obtiene las estadísticas de almacenamiento del usuario
        public async Task<StorageStatisticsResponseDto> GetUserStorageStatsAsync(int ownerId, CancellationToken cToken)
        {
            //Obtener las estadísticas de almacenamiento del usuario
            (int TotalFiles, long TotalSizeBytes, int TotalFolders) stats = await _fileItemRepository.GetUserStatsAsync(ownerId, cToken);

            //Mapear las estadísticas a StorageStatisticsResponseDto
            return new StorageStatisticsResponseDto(stats.TotalFiles,
                                                    stats.TotalFolders,
                                                    stats.TotalSizeBytes,
                                                    Math.Round((double)stats.TotalSizeBytes / (1024 * 1024), 2)); //Tamaño en MB con 2 decimales
        }
    }
}