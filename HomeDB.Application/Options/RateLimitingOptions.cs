using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.Options
{
    //Para la configuración de limitación de Rate Limiting en la aplicación.
    public sealed class RateLimitingOptions : IValidatableObject
    {
        //Nombre de la sección(usado en el program.cs)
        public const string SectionName = "RateLimiting";

        public RateLimiterPolicySettings Global { get; set; } = new RateLimiterPolicySettings();

        public RateLimiterPolicySettings Auth { get; set; } = new RateLimiterPolicySettings();

        //Nota, no se puede hacer con [Required] ya que ValidateDataAnnotations() no valida las propiedades anidadas.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Global
            if (Global.Enabled)
            {
                if (Global.TokenLimit < 1)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Global:TokenLimit debe ser mayor que 0.", new[] { nameof(Global) });
                }
                if (Global.TokensPerPeriod < 1)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Global:TokensPerPeriod debe ser mayor que 0.", new[] { nameof(Global) });
                }
                if (Global.ReplenishmentPeriod <= TimeSpan.Zero)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Global:ReplenishmentPeriod debe ser mayor que cero.", new[] { nameof(Global) });
                }
            }


            //Auth
            if (Auth.Enabled)
            {
                if (Auth.TokenLimit < 1)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Auth:TokenLimit debe ser mayor que 0.", new[] { nameof(Auth) });
                }
                if (Auth.TokensPerPeriod < 1)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Auth:TokensPerPeriod debe ser mayor que 0.", new[] { nameof(Auth) });
                }
                if (Auth.ReplenishmentPeriod <= TimeSpan.Zero)
                {
                    yield return new ValidationResult(
                        "RateLimiting:Auth:ReplenishmentPeriod debe ser mayor que cero.", new[] { nameof(Auth) });
                }
            }

        }
    }

    //Configuración de un rate limiter.
    public class RateLimiterPolicySettings
    {
        public bool Enabled { get; set; } = true;
        public int TokenLimit { get; set; }

        public int TokensPerPeriod { get; set; }

        public TimeSpan ReplenishmentPeriod { get; set; }
    }
}