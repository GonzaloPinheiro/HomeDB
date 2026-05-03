
namespace HomeDB.Domain.Exceptions
{
    public class InvalidRefreshTokenException : Exception
    {
        public InvalidRefreshTokenException()
       : base("Refresh token is invalid, expired or has already been used.") { }
    }
}
