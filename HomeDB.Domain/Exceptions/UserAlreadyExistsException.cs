
namespace HomeDB.Domain.Exceptions
{

    /// <summary>
    /// Intento de crear un usuario que ya existe
    /// </summary>
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string username)
            : base($"User '{username}' already exists.") { }
    }
}