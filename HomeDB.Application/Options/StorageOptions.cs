using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    //Para la comprobación de la configuración de almacenamiento en el arranque, se necesitan la ruta base y el tamaño máximo de archivo.
    public class StorageOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "La ruta base de almacenamiento no puede estar vacía.")]
        public string BasePath { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "El tamaño máximo de archivo debe ser un valor positivo.")]
        public long? MaxFileSizeBytes { get; set; } = null;

        [Range(1, long.MaxValue, ErrorMessage = "El límite de almacenamiento debe ser un valor positivo.")]
        public long? StorageLimitBytes { get; set; } = null;
    }
}
