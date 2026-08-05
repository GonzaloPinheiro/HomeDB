
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
        EmailAlreadyExists = 1011,
        RoleNotFound = 1012,
        FolderCyclicReference = 1013,
        PermissionsNotFound = 1014,
        UserSettingsNotFound = 1015,
        StorageLimitExceeded = 1016,
        UserHasAssociatedData = 1017,
        //Códigos de error relacionados con la subida de archivos
        UploadSessionNotFound = 1018,
        UploadIncomplete = 1019,
        UploadSessionNotActive = 1020,
        InvalidChunkSize = 1021,
        InvalidChunkNumber = 1022,
        AssembledFileSizeMismatch = 1023,
        InvalidUploadRequest = 1024,

        //Codigos de errores críticos
        InternalError = 9999
    }
}