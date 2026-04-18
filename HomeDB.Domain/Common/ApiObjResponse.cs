
namespace HomeDB.Domain.Common
{
    /// <summary>
    /// Objeto de respuesta estandar para las respuestas de la API.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiObjResponse<T>
    {
        public bool Result { get; set; }
        public T? Data { get; set; }
        public ApiErrorCodes? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Constructor que se utiliza para crear una respuesta de éxito con los datos devueltos por la API.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ApiObjResponse<T> Success(T? data) => new()
        {
            Result = true,
            Data = data,
            ErrorCode = null,
            ErrorMessage = null
        };

        /// <summary>
        /// Constructor que se utiliza para crear una respuesta de error con un código de error y un mensaje de error.
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static ApiObjResponse<T> Failure(ApiErrorCodes errorCode, string errorMessage) => new()
        {
            Result = false,
            Data = default,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
