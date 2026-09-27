
namespace HomeDB.Domain.Interfaces.Services
{
    public interface IPasswordHelper
    {
        /// <summary>
        /// Hashea la contraseña recibida
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifica integridad de la contraseña recibida
        /// </summary>
        bool VerifyPassword(string password, string storedHash);

        /// <summary>
        /// Hashea el refreshToken recibido
        /// </summary>
        string HashRefreshToken(string refreshToken);
    }
}