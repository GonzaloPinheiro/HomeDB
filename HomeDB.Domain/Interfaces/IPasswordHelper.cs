
namespace HomeDB.Domain.Interfaces
{
    public interface IPasswordHelper
    {
        //Hashea la contraseña recibida
        string HashPassword(string password);
        //Verifica integridad de la contraseña recibida
        bool VerifyPassword(string password, string storedHash);
        //Hashea el refreshToken recibido
        string HashRefreshToken(string refreshToken);
    }
}