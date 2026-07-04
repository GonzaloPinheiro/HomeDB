
namespace HomeDB.Domain.Exceptions
{
    public class UserHasAssociatedDataException : Exception
    {
        public UserHasAssociatedDataException(int userId)
            : base($"User with ID '{userId}' has associated data and cannot be deleted.") { }
    }
}