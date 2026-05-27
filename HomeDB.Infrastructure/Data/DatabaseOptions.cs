using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Data
{
    //Para la comprobación de la conexión a la base de datos en el arranque, se necesita una cadena de conexión completa.
    public class DatabaseOptions
    {
        [Required]
        [MinLength(1)]
        public string PostgreSQL_HomeDB { get; set; } = string.Empty;
    }
}