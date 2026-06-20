
namespace HomeDB.Domain.Common.Snapshots
{
    /// <summary>
    /// Snapshot para la temperatura de la CPU del sistema
    /// </summary>
    public class TemperatureSnapshot
    {
        public double CelsiusDegrees { get; init; }

        public TemperatureSnapshot(double celsiusDegrees)
        {
            CelsiusDegrees = celsiusDegrees;
        }
    }
}