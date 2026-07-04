using HomeDB.Domain.Common.Snapshots;

namespace HomeDB.Domain.Entities
{
    public class SystemMetricsEntry
    {
        public int Id { get; init; } //PK
        public DateTimeOffset Timestamp { get; init; } = DateTime.UtcNow; //Momento de la lectura
        //CPU
        public double? CpuUsagePercent { get; init; }//% uso de CPU

        //MEMORY
        public long? MemoryTotalBytes { get; init; } //Cantidad de RAM disponible
        public long? MemoryUsedBytes { get; init; } //Cantidad de RAM en uso
        public double? MemoryUsagePercent { get; init; } //% cantidad de RAM en uso

        //DISK
        public long? DiskTotalBytes { get; init; } //Cantidad de espacio en disco
        public long? DiskUsedBytes { get; init; } //Cantidad de espacio usado en disco
        public double? DiskUsagePercent { get; init; } //% cantidad de espacio usado en disco

        //TEMPERATURE
        public double? TemperatureCelsius { get; init; } //Temperatura de la CPU

        //FAN
        public bool? FanIsRunning { get; init; }
        public int? FanRpmSpeed { get; init; }
        public int? FanPwmDutyCycle {  get; init; }
        public string? FanControlMode { get; init; }

        //Constructor vacío requerido por EF Core para materializar la entidad
        private SystemMetricsEntry() { }

        //Construcores
        public SystemMetricsEntry(
            CpuSnapshot? cpu,
            MemorySnapshot? memory,
            DiskSnapshot? disk,
            TemperatureSnapshot? temperature,
            FanStatusSnapshot? fanStatus)
        {
            CpuUsagePercent = cpu?.UsagePercent;
            MemoryTotalBytes = memory?.TotalBytes;
            MemoryUsedBytes = memory?.UsedBytes;
            MemoryUsagePercent = memory?.UsagePercent;
            DiskTotalBytes = disk?.TotalBytes;
            DiskUsedBytes = disk?.UsedBytes;
            DiskUsagePercent = disk?.UsagePercent;
            TemperatureCelsius = temperature?.CelsiusDegrees;
            FanIsRunning = fanStatus?.IsRunning;
            FanRpmSpeed = fanStatus?.RpmSpeed;
            FanPwmDutyCycle = fanStatus?.PwmDutyCycle;
            FanControlMode = fanStatus?.ControlMode;
        }
    }
}