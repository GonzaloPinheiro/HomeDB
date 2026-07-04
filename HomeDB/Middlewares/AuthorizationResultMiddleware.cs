using HomeDB.Domain.Common;

namespace HomeDB.Middlewares
{
    //Middleware que intercepta las respuestas con código de estado 403 (Forbidden) y devuelve un mensaje de error en formato ApiObjResponse
    public class AuthorizationResultMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationResultMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //Continua por el pipeline de middlewares
            await _next(context);

            //Si la respuesta es 403 y no se ha iniciado la respuesta, devuelve un mensaje de error en formato ApiObjResponse
            if (context.Response.StatusCode == StatusCodes.Status403Forbidden && !context.Response.HasStarted)
            {
                //Establece el tipo de contenido de la respuesta a JSON
                context.Response.ContentType = "application/json";

                ApiObjResponse<object> response = ApiObjResponse<object>.Failure(
                    ApiErrorCodes.Unauthorized,
                    "You do not have access to this module.");

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}