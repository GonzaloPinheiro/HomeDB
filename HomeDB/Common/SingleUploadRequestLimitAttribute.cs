using HomeDB.Application.Options;
using HomeDB.Domain.Exceptions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace HomeDB.Common
{
    /// <summary>
    /// Aplica a la subida directa el límite de Storage:SingleUploadMaxFileBytes. Sustituye a [RequestSizeLimit] y
    /// [RequestFormLimits], que solo admiten constantes de compilación y no pueden leer el appsettings.
    /// Se ejecuta antes del model binding y hace tres cosas:
    /// 1. Rechaza con FileTooLargeException (413, ApiObjResponse) una petición cuyo Content-Length declarado supera
    ///    el límite. Sin esto, cuando Kestrel aplica el límite en mitad del binding, [ApiController] lo convierte
    ///    en un 400 genérico que no es un ApiObjResponse.
    /// 2. Fija el máximo del cuerpo de la petición en Kestrel (protege también si no hay Content-Length, p. ej.
    ///    transfer-encoding chunked). Sin esto valdría el límite global de 500 MB.
    /// 3. Fija el límite de multipart de esta petición, igual que haría [RequestFormLimits].
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SingleUploadRequestLimitAttribute : Attribute, IAsyncResourceFilter
    {
        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            StorageOptions options = context.HttpContext.RequestServices.GetRequiredService<IOptions<StorageOptions>>().Value;
            long maxRequestBytes = options.SingleUploadMaxRequestBytes;

            //1. Rechazo por Content-Length declarado, antes de leer el cuerpo
            long? contentLength = context.HttpContext.Request.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxRequestBytes)
                throw new FileTooLargeException(contentLength.Value, options.SingleUploadMaxFileBytes);

            //2. Límite del cuerpo en Kestrel (el feature no existe en TestServer o es de solo lectura si el cuerpo ya se empezó a leer)
            IHttpMaxRequestBodySizeFeature? bodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature is { IsReadOnly: false })
                bodySizeFeature.MaxRequestBodySize = maxRequestBytes;

            //3. Límite de multipart de esta petición (mismo mecanismo que [RequestFormLimits])
            IFormFeature? formFeature = context.HttpContext.Features.Get<IFormFeature>();
            if (formFeature is null || formFeature.Form is null)
            {
                FormOptions formOptions = new FormOptions { MultipartBodyLengthLimit = maxRequestBytes };
                context.HttpContext.Features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, formOptions));
            }

            return next();
        }
    }
}
