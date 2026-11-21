
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Una ubicación de almacenamiento no tiene espacio para lo que se quiere guardar: por el espacio libre mínimo del
    /// disco (MinFreeBytes) o por el límite de uso de la ubicación (MaxUsedBytes). ExceptionHandlerMiddleware expone el
    /// mensaje tal cual al cliente, así que el motivo debe ser genérico: nunca incluir rutas del host.
    /// </summary>
    public class StorageLocationFullException : Exception
    {
        public string LocationKey { get; }
        public string Reason { get; }

        public StorageLocationFullException(string locationKey, string reason)
            : base($"Storage location '{locationKey}' cannot hold this file: {reason}.")
        {
            LocationKey = locationKey;
            Reason = reason;
        }
    }
}
