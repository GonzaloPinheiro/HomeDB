using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces
{
    public interface IJwtService
    {
        //Generates a JWT access token for the given user
        string GenerateAccessToken(User user);
        //Genera un refresh token
        string GenerateRefreshToken();
    }
}