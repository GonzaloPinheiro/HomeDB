
namespace HomeDB.Domain.Common.Snapshots
{
    /// <summary>
    /// Snapshot para el estado del ventilador del sistema
    /// </summary>
    public class FanStatusSnapshot
    {
        public bool IsRunning { get; init; }
        public int? RpmSpeed { get; init; }
        public int? PwmDutyCycle { get; init; }
        public string? ControlMode { get; init; }

        public FanStatusSnapshot(bool isRunning, int? rpmSpeed, int? pwmDutyCycle, string? controlMode)
        {
            IsRunning = isRunning;
            RpmSpeed = rpmSpeed;
            PwmDutyCycle = pwmDutyCycle;
            ControlMode = controlMode;
        }
    }
}