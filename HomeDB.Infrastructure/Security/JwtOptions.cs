using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Security
{
    //Para la comprobación de la autenticación JWT en el arranque, se necesitan el issuer y la key completos.
    public class JwtOptions
    {
        [Required]
        [MinLength(32, ErrorMessage = "JWT Key debe tener al menos 32 caracteres (256 bits).")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "JWT Issuer debe tener al menos 8 caracteres.")]
        public string Issuer { get; set; } = string.Empty;
    }
}