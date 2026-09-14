using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    //Configuración del subsistema de backups: directorio de origen común y ajustes propios de cada nivel de backup (Daily, Monthly, ...).
    public class BackupOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "El directorio de origen del backup no puede estar vacío.")]
        public string SourceDirectory { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int CheckIntervalMinutes { get; set; } = 60;

        public BackupLevelOptions Daily { get; set; } = new BackupLevelOptions();
    }

    //Ajustes independientes de un nivel de backup concreto: directorio destino, hora de ejecución (UTC) e intervalo mínimo entre backups.
    public class BackupLevelOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "El directorio del backup no puede estar vacío.")]
        public string Directory { get; set; } = string.Empty;

        [Range(0, 23)]
        public int RunAtHourUtc { get; set; } = 3;

        [Range(1, 168)]
        public int IntervalHours { get; set; } = 24;
    }
}
