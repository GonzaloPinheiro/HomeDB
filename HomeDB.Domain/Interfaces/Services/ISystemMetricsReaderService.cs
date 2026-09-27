using HomeDB.Domain.Common.Snapshots;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface ISystemMetricsReaderService
    {
        /// <summary>
        /// 
        /// </summary>
        Task<CpuSnapshot?> ReadCpuAsync(CancellationToken cToken);

        /// <summary>
        /// 
        /// </summary>
        Task<MemorySnapshot?> ReadMemoryAsync(CancellationToken cToken);

        /// <summary>
        /// 
        /// </summary>
        Task<DiskSnapshot?> ReadDiskAsync(CancellationToken cToken);

        /// <summary>
        /// 
        /// </summary>
        Task<TemperatureSnapshot?> ReadTemperatureAsync(CancellationToken cToken);

        /// <summary>
        /// 
        /// </summary>
        Task<FanStatusSnapshot?> ReadFanStatusAsync(CancellationToken cToken);
    }
}