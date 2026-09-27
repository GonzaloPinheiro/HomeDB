using HomeDB.Domain.Common.Enums;

namespace HomeDB.Domain.Exceptions
{
    public class BackupAlreadyRunningException : Exception
    {
        public BackupAlreadyRunningException(BackupLevel level)
            : base($"A backup of level '{level}' is already running.") { }
    }
}
