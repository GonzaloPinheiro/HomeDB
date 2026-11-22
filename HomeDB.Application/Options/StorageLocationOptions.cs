using System.ComponentModel.DataAnnotations;
using HomeDB.Domain.Common;

namespace HomeDB.Application.Options
{
    public class StorageLocationOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "La clave de la ubicación no puede estar vacía.")]
        public string Key { get; set; } = string.Empty;

        public LocationUsage Usage { get; set; }

        //Entre todas las ubicaciones que compartan el mismo Usage, exactamente una debe marcar esto para que
        //el resolver sepa cuál usar cuando se pide "la ubicación por defecto para este uso".
        public bool IsDefault { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "La ruta relativa de la ubicación no puede estar vacía.")]
        public string SubPath { get; set; } = string.Empty;

        public bool IncludeInBackup { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "El límite de uso de la ubicación debe ser un valor positivo.")]
        public long? MaxUsedBytes { get; set; } = null;
    }
}
