using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    //Para el proceso de backup diario: directorio de origen de los archivos y directorio donde se almacenan los backups generados.
    public class BackupOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "El directorio de origen del backup no puede estar vacío.")]
        public string SourceDirectory { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "El directorio de backups diarios no puede estar vacío.")]
        public string DailyDirectory { get; set; } = string.Empty;
    }
}
