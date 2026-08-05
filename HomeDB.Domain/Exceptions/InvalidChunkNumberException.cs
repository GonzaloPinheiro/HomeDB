
namespace HomeDB.Domain.Exceptions
{
    public class InvalidChunkNumberException : Exception
    {
        public InvalidChunkNumberException(int chunkNumber, int totalChunks)
            : base($"Invalid chunk number: {chunkNumber}. Valid range is 1 to {totalChunks}.") { }
    }
}
