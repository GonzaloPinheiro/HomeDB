
namespace HomeDB.Domain.Exceptions
{
    public class UserModulePermissionsNotFoundException : Exception
    {
        public UserModulePermissionsNotFoundException(int userId)
            : base($"Module permissions not found for user {userId}.") { }
    }
}