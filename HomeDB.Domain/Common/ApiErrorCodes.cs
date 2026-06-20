
namespace HomeDB.Domain.Common
{
    /// <summary>
    /// Proporciona un conjunto de códigos de error estandar para las respuestas de la API.
    /// </summary>
    public enum ApiErrorCodes
    {
        FileNotFound = 1001,
        FolderNotFound = 1002,
        Unauthorized = 1003,
        FileTooLarge = 1004,
        FolderNotEmpty = 1005,
        InvalidCredentials = 1006,
        UserAlreadyExists = 1007,
        UserNotFound = 1008,
        RateLimitExceeded = 1009,
        MetricNotFound = 1010,
        InternalError = 9999
    }
}