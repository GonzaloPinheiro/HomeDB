
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// El disco que respalda una ubicación de almacenamiento no está disponible (no montado, marcador ausente
    /// o incorrecto, o sin permiso de escritura). ExceptionHandlerMiddleware expone el mensaje tal cual al cliente,
    /// así que el motivo debe ser genérico: nunca incluir rutas del host.
    /// </summary>
    public class StorageLocationUnavailableException : Exception
    {
        public string DiskKey { get; }
        public string Reason { get; }

        public StorageLocationUnavailableException(string diskKey, string reason)
            : base($"Storage disk '{diskKey}' is not available: {reason}.")
        {
            DiskKey = diskKey;
            Reason = reason;
        }
    }
}
