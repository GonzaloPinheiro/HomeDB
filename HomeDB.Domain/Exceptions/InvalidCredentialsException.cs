
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Credenciales incorrectas en login
    /// </summary>
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
            : base("Invalid username or password.") { }
    }
}