
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Usuario no encontrado
    /// </summary>
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string username)
            : base($"User '{username}' was not found.") { }

        public UserNotFoundException(int id)
            : base($"User with id {id} was not found.") { }
    }
}