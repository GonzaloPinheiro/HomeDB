using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    //Para el fallback a fichero cuando falla la persistencia de logs en la base de datos.
    public class LogFallbackOptions
    {
        [Required]
        [MinLength(1, ErrorMessage = "La ruta base de fallback de logs no puede estar vacía.")]
        public string BasePath { get; set; } = string.Empty;
    }
}