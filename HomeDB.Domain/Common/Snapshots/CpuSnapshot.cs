
namespace HomeDB.Domain.Common.Snapshots
{
    /// <summary>
    /// Snapchot para el uso de la cpu del sistema
    /// </summary>
    public class CpuSnapshot
    {
        public double UsagePercent { get; init; }

        public CpuSnapshot(double usagePercent)
        {
            UsagePercent = usagePercent;
        }
    }
}