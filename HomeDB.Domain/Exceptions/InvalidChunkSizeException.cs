
namespace HomeDB.Domain.Exceptions
{
    //Excepción usada para indicar que el tamaño de un chunk recibido no coincide con el tamaño esperado.
    public class InvalidChunkSizeException : Exception
    {
        public InvalidChunkSizeException(int chunkNumber, long expectedSize, long actualSize) 
            : base($"Invalid chunk size for chunk {chunkNumber}. Expected: {expectedSize} bytes, Actual: {actualSize} bytes.") {}
    }
}