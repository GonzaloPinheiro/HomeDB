
using HomeDB.Domain.Common.Snapshots;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Infrastructure.SystemMonitoring
{
    public class FakeSystemMetricsReader : ISystemMetricsReaderService
    {
        private readonly Random _random;

        public FakeSystemMetricsReader()
        {
            // Valor aleatorio para datos dinámicos
            _random = new Random();
        }

        /// <summary>
        /// Devuelve un dato de uso de CPU falso
        /// </summary>
        public Task<CpuSnapshot?> ReadCpuAsync(CancellationToken cToken)
        {
            double usagePercent = _random.NextDouble() * 55 + 5;
            return Task.FromResult<CpuSnapshot?>(new CpuSnapshot(usagePercent));
        }

        /// <summary>
        /// Genera unos datos de uso de memoria RAM falsos
        /// </summary>
        public Task<MemorySnapshot?> ReadMemoryAsync(CancellationToken cToken)
        {
            long totalBytes = 16L * 1024 * 1024 * 1024;
            double usagePercent = _random.NextDouble() * 40 + 30;
            long usedBytes = (long)(totalBytes * (usagePercent / 100.0));

            return Task.FromResult<MemorySnapshot?>(new MemorySnapshot(totalBytes, usedBytes, usagePercent));
        }

        /// <summary>
        /// Genera unos datos del esado del disco falsos
        /// </summary>
        public Task<DiskSnapshot?> ReadDiskAsync(CancellationToken cToken)
        {
            long totalBytes = 512L * 1024 * 1024 * 1024;
            double usagePercent = _random.NextDouble() * 40 + 40;
            long usedBytes = (long)(totalBytes * (usagePercent / 100.0));

            return Task.FromResult<DiskSnapshot?>(new DiskSnapshot(totalBytes, usedBytes, usagePercent));
        }

        /// <summary>
        /// Genera un valor de temeratura de CPU falso
        /// </summary>
        public Task<TemperatureSnapshot?> ReadTemperatureAsync(CancellationToken cToken)
        {
            double celsius = _random.NextDouble() * 30 + 35;
            return Task.FromResult<TemperatureSnapshot?>(new TemperatureSnapshot(celsius));
        }
    }
}