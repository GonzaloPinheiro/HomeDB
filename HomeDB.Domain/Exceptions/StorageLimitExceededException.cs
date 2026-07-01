
namespace HomeDB.Domain.Exceptions
{
    public class StorageLimitExceededException : Exception
    {
        public StorageLimitExceededException(long usedBytes, long limitBytes)
            : base($"Storage limit of {limitBytes} bytes exceeded. Current usage would reach {usedBytes} bytes.") { }
    }
}
