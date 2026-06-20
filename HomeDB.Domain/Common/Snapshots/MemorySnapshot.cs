
namespace HomeDB.Domain.Common.Snapshots
{
    /// <summary>
    /// Snapshot para el uso de memoria RAM del sistema
    /// </summary>
    public class MemorySnapshot
    {
        public long TotalBytes { get; init; }
        public long UsedBytes { get; init; }
        public double UsagePercent { get; init; }

        public MemorySnapshot(long totalBytes, long usedBytes, double usagePercent)
        {
            TotalBytes = totalBytes;
            UsedBytes = usedBytes;
            UsagePercent = usagePercent;
        }
    }
}