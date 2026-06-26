
namespace HomeDB.Domain.Exceptions
{
    public class EmailAlreadyExistsException : Exception
    {
        public EmailAlreadyExistsException(string email)
            : base($"Email '{email}' is already in use.") { }
    }
}