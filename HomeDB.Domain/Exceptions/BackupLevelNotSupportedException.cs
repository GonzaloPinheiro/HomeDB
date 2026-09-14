using HomeDB.Domain.Common.Enums;

namespace HomeDB.Domain.Exceptions
{
    public class BackupLevelNotSupportedException : Exception
    {
        public BackupLevelNotSupportedException(BackupLevel level)
            : base($"Backup level '{level}' is not supported yet.") { }
    }
}
