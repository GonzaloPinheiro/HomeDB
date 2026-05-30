
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Storage
{
    //Para la comprobación de la configuración de almacenamiento en el arranque, se necesitan la ruta base y el tamaño máximo de archivo.
    public class StorageOptions
    {
        [Required]
        [MinLength(1)]
        public string BasePath { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue)]
        public long? MaxFileSizeBytes { get; set; } = null;
    }
}