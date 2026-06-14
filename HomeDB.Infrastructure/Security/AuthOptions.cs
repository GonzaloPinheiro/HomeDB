using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Infrastructure.Security
{
    public class AuthOptions
    {
        [Required]
        public SameSiteMode? CookieSameSite { get; set; }
    }
}
