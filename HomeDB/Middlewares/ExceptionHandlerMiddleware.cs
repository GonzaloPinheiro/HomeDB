using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Infrastructure.Observability;

namespace HomeDB.Middlewares
{

    /// <summary>
    /// Middleware global que captura todas las excepciones no manejadas
    /// y las convierte en respuestas HTTP apropiadas
    /// </summary>
    public class ExceptionHandlerMiddleware
    {
        //Delegado que apunta al siguiente middleware en el pipeline
        private readonly RequestDelegate _next;

        //Logger para registrar las excepciones
        private readonly Logger _logger;

        #region Constructores
        public ExceptionHandlerMiddleware(RequestDelegate next, Logger logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Variables y objetos
            string? correlationId;
            string path = context.Request.Path;
            string method = context.Request.Method;

            // Verificar si ya existe un correlationId
            if (context.Items.ContainsKey("CorrelationId") && context.Items["CorrelationId"] != null)
            {
                // Ya existe, usarlo
                correlationId = context.Items["CorrelationId"]!.ToString();
            }
            else
            {
                // No existe, generar uno nuevo
                correlationId = Guid.NewGuid().ToString();
                context.Items["CorrelationId"] = correlationId;
            }

            try
            {
                //Hacer que el request siga su camino normal a través del pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                //Mapear la excepción con la indormación para la respuesta http y
                //el log dependiendo del tipo de excepción
                var (statusCode, errorMessage, errorCode, logLevel, logMessage) = ex switch
                {
                    FileItemNotFoundException filenfe => (
                        StatusCodes.Status404NotFound,
                        filenfe.Message,
                        ApiErrorCodes.FileNotFound,
                        "Warning",
                        $"Archivo no encontrado. Path: {path}, Method: {method}"
                    ),

                    ParentFolderNotFoundException pfnt =>(
                        StatusCodes.Status404NotFound,
                        pfnt.Message,
                        ApiErrorCodes.FolderNotFound,
                        "Warning",
                        $"Carpeta padre no encontrada. Path: {path}, Method: {method}"
                    ),

                    FolderNotFoundException foldnte => (
                        StatusCodes.Status404NotFound,
                        foldnte.Message,
                        ApiErrorCodes.FolderNotFound,
                        "Warning",
                        $"Carpeta no encontrada. Path: {path}, Method: {method}"
                    ),

                    FolderNotEmptyException foldnee => (
                        StatusCodes.Status400BadRequest,
                        foldnee.Message,
                        ApiErrorCodes.FolderNotEmpty,
                        "Warning",
                        $"Carpeta no vacía. Path: {path}, Method: {method}"
                    ),

                    UserNotFoundException usernte => (
                        StatusCodes.Status404NotFound,
                        usernte.Message,
                        ApiErrorCodes.UserNotFound,
                        "Warning",
                        $"Usuario no encontrado. Path: {path}, Method: {method}"
                    ),

                    RoleNotFoundException rolente => (
                        StatusCodes.Status404NotFound,
                        rolente.Message,
                        ApiErrorCodes.RoleNotFound,
                        "Warning",
                        $"Rol no encontrado. Path: {path}, Method: {method}"
                    ),

                    MetricNotFoundException mnfe => (
                         StatusCodes.Status404NotFound,
                         mnfe.Message,
                         ApiErrorCodes.MetricNotFound,
                         "Warning",
                         $"No ha sido posible encontrar la métrica indicada. Path: {path}, Method: {method}"
                     ),

                    UserAlreadyExistsException useraee => (
                        StatusCodes.Status409Conflict,
                        useraee.Message,
                        ApiErrorCodes.UserAlreadyExists,
                        "Warning",
                        $"Usuario ya existe. Path: {path}, Method: {method}"
                    ),
                    EmailAlreadyExistsException emailaee => (
                        StatusCodes.Status409Conflict,
                        emailaee.Message,
                        ApiErrorCodes.EmailAlreadyExists,
                        "Warning",
                        $"Usuario ya existe. Path: {path}, Method: {method}"
                    ),

                    InvalidCredentialsException ice => (
                        StatusCodes.Status401Unauthorized,
                        ice.Message,
                        ApiErrorCodes.InvalidCredentials,
                        "Warning",
                        $"Credenciales inválidas. Path: {path}, Method: {method}"
                    ),
                    InvalidRefreshTokenException irte => (
                        StatusCodes.Status401Unauthorized,
                        irte.Message,
                        ApiErrorCodes.InvalidCredentials,
                        "Warning",
                        $"Refresh token inválido o expirado. Path: {path}, Method: {method}"
                    ),
                    UnauthorizedException ue => (
                        StatusCodes.Status403Forbidden,
                        ue.Message,
                        ApiErrorCodes.Unauthorized,
                        "Warning",
                        $"Acceso no autorizado. Path: {path}, Method: {method}"
                    ),

                    UnauthorizedAccessException uae => (
                        StatusCodes.Status403Forbidden,
                        uae.Message,
                        ApiErrorCodes.Unauthorized,
                        "Warning",
                        $"Acceso no autorizado. Path: {path}, Method: {method}"
                    ),
                    ArgumentNullException ane => (
                        StatusCodes.Status400BadRequest,
                        "Parámetro requerido no proporcionado",
                        ApiErrorCodes.InternalError,
                        "Warning",
                        $"Argumento nulo. Parámetro: {ane.ParamName}, Path: {path}, Method: {method}"
                    ),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "Error inesperado del servidor",
                        ApiErrorCodes.InternalError,
                        "Critical",
                        $"Excepción no controlada. Type: {ex.GetType().Name}, Path: {path}, Method: {method}, Message: {ex.Message}"
                    )
                };

                await LogAsync(ex, logLevel, logMessage, correlationId!);
                await WriteResponseAsync(context, statusCode, errorCode, errorMessage);
            }
        }

        #region Helpers
        /// <summary>
        /// Agrega una entrada de log con la información de la excepción capturada
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private async Task LogAsync(Exception ex, string level, string message, string correlationId)
        {
            await _logger.AddAsync(new LogEntry
            {
                Level = level,
                Source = "HomeDB.Middleware.ExceptionHandlerMiddleware",
                Operation = "InvokeAsync",
                Message = message,
                CorrelationId = correlationId,
                Exception = ex.ToString()
            });
        }

        /// <summary>
        /// Define la respuesta HTTP que se enviará al cliente en caso de una excepción, incluyendo el código de estado, el código de error y el mensaje de error
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private static async Task WriteResponseAsync(HttpContext context, int statusCode, ApiErrorCodes errorCode, string errorMessage)
        {
            var response = ApiObjResponse<object>.Failure(errorCode, errorMessage);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
        #endregion

    }
}