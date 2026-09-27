using HomeDB.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    public class AuthOptions
    {
        [Required]
        public SameSitePolicy? CookieSameSite { get; set; }
    }
}
