using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Data
{
    //Para la comprobación de la conexión a la base de datos en el arranque, se necesita una cadena de conexión completa.
    public class DatabaseOptions
    {
        [Required(ErrorMessage = "La cadena de conexión PostgreSQL_HomeDB no está creada.")]
        [MinLength(1, ErrorMessage = "La cadena de conexión a PostgreSQL no puede estar vacía.")]
        public string PostgreSQL_HomeDB { get; set; } = string.Empty;
    }
}