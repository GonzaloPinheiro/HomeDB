using Microsoft.AspNetCore.Mvc;

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
    }
}
