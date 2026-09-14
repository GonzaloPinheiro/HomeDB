using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT access token for the given user
        /// </summary>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Genera un refresh token
        /// </summary>
        string GenerateRefreshToken();
    }
}