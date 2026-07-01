
namespace HomeDB.Domain.Exceptions
{
    public class FileTooLargeException : Exception
    {
        public FileTooLargeException(long sizeBytes, long limitBytes)
            : base($"File size {sizeBytes} bytes exceeds the limit of {limitBytes} bytes.") { }
    }
}
