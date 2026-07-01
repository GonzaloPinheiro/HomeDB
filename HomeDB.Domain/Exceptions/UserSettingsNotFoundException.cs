
namespace HomeDB.Domain.Exceptions
{
    public class UserSettingsNotFoundException : Exception
    {
        public UserSettingsNotFoundException(int userId)
            : base($"Settings not found for user {userId}.") { }
    }
}