using System.ComponentModel.DataAnnotations;
using HomeDB.Domain.Common;

namespace HomeDB.Application.Options
{
    public class DiskOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "La clave del disco no puede estar vacía.")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "La ruta del disco no puede estar vacía.")]
        public string Path { get; set; } = string.Empty;

        public DiskStartupPolicy StartupPolicy { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "El espacio libre mínimo debe ser un valor positivo o cero.")]
        public long MinFreeBytes { get; set; }

        [MinLength(1, ErrorMessage = "Un disco debe declarar al menos una ubicación.")]
        public List<StorageLocationOptions> Locations { get; set; } = new();
    }
}
