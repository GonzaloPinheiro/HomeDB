
namespace HomeDB.Domain.Common.Enums
{
    /// <summary>
    /// Política SameSite aplicable a las cookies de autenticación. Se traduce a Microsoft.AspNetCore.Http.SameSiteMode en la capa web.
    /// </summary>
    public enum SameSitePolicy
    {
        None = 1,
        Lax = 2,
        Strict = 3
    }
}
