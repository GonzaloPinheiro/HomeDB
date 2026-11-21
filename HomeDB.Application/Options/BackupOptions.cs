using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Options
{
    //Configuración del subsistema de backups: intervalo de comprobación y ajustes propios de cada nivel de backup (Daily, Monthly, ...).
    //Qué se copia (ubicaciones con IncludeInBackup) y dónde se guarda (ubicación por defecto de uso Backups) lo define Storage:Disks.
    public class BackupOptions
    {
        [Range(1, 1440)]
        public int CheckIntervalMinutes { get; set; } = 60;

        [ValidateObjectMembers]
        public BackupLevelOptions Daily { get; set; } = new BackupLevelOptions();
    }

    //Ajustes independientes de un nivel de backup concreto: hora de ejecución (UTC) e intervalo mínimo entre backups.
    public class BackupLevelOptions
    {
        [Range(0, 23)]
        public int RunAtHourUtc { get; set; } = 3;

        [Range(1, 168)]
        public int IntervalHours { get; set; } = 24;
    }
}
