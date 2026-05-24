using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeDB.Controllers
{
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Obtiene el correlationId del request actual (generado por el middleware)
        /// </summary>
        protected string GetCorrelationId()
        {
            return HttpContext.Items["CorrelationId"]!.ToString()!;
        }

        /// <summary>
        /// Obtiene el userId del token JWT del request actual
        /// </summary>
        protected int GetUserId()
        {
            return int.Parse(User.FindFirstValue("userId")!);
        }

        /// <summary>
        /// Obtiene la ip address del request actual
        /// </summary>
        /// <returns></returns>
        protected string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}