using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Security
{
    //Para la comprobación de la autenticación JWT en el arranque, se necesitan el issuer y la key completos.
    public class JwtOptions
    {
        [Required]
        [MinLength(1)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string Issuer { get; set; } = string.Empty;
    }
}