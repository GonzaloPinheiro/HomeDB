using HomeDB.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeDB.Common
{
    /// <summary>
    /// Rechaza antes del model binding una petición cuyo Content-Length declarado supera el máximo, con
    /// FileTooLargeException (413, ApiObjResponse). Complementa a [RequestSizeLimit]: cuando Kestrel aplica ese
    /// límite en mitad del binding, [ApiController] convierte el fallo en un 400 genérico que no es un
    /// ApiObjResponse. Una petición sin Content-Length (transfer-encoding chunked) no se puede comprobar aquí y
    /// sigue protegida por [RequestSizeLimit].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequestSizeGuardAttribute : Attribute, IAsyncResourceFilter
    {
        private readonly long _maxRequestBytes;
        private readonly long _reportedLimitBytes;

        /// <param name="maxRequestBytes">Tamaño máximo de la petición completa.</param>
        /// <param name="reportedLimitBytes">Límite que se comunica al cliente en el error (el del archivo).</param>
        public RequestSizeGuardAttribute(long maxRequestBytes, long reportedLimitBytes)
        {
            _maxRequestBytes = maxRequestBytes;
            _reportedLimitBytes = reportedLimitBytes;
        }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            long? contentLength = context.HttpContext.Request.ContentLength;

            if (contentLength.HasValue && contentLength.Value > _maxRequestBytes)
                throw new FileTooLargeException(contentLength.Value, _reportedLimitBytes);

            return next();
        }
    }
}
