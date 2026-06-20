using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.SystemMonitoring
{
    //Se usa para verificar que la configuración de las variables de entorno esté bien hecha
    public class SystemMetricsOptions
    {
        [Range(1, 1440)]
        public int SampleIntervalMinutes { get; set; } = 5;

        [Range(1, 365)]
        public int RetentionDays { get; set; } = 30;
    }
}
