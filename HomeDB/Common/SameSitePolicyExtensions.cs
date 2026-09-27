using HomeDB.Domain.Common.Enums;

namespace HomeDB.Common
{
    public static class SameSitePolicyExtensions
    {
        /// <summary>
        /// Traduce la política SameSite de dominio (agnóstica de framework) al tipo que exige la API de cookies de ASP.NET Core.
        /// </summary>
        public static SameSiteMode ToSameSiteMode(this SameSitePolicy policy) => policy switch
        {
            SameSitePolicy.Strict => SameSiteMode.Strict,
            SameSitePolicy.Lax => SameSiteMode.Lax,
            SameSitePolicy.None => SameSiteMode.None,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Política SameSite no soportada.")
        };
    }
}
