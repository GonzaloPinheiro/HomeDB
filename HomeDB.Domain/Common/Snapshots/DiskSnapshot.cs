
namespace HomeDB.Domain.Common.Snapshots
{
    /// <summary>
    /// Snapshot para el espacio en disco del sistema
    /// </summary>
    public class DiskSnapshot
    {
        public long TotalBytes { get; init; }
        public long UsedBytes { get; init; }
        public double UsagePercent { get; init; }

        public DiskSnapshot(long totalBytes, long usedBytes, double usagePercent)
        {
            TotalBytes = totalBytes;
            UsedBytes = usedBytes;
            UsagePercent = usagePercent;
        }
    }
}