
namespace HomeDB.Domain.Exceptions
{
    /// <summary>
    /// Intento de acceder a un recurso que no pertenece al usuario
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(int userId)
            : base($"User {userId} is not authorized to access this resource.") { }

        public UnauthorizedException(int userId, int resourceId)
            : base($"User {userId} is not authorized to access resource {resourceId}.") { }
    }
}