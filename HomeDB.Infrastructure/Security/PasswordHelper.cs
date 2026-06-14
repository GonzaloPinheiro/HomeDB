using HomeDB.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace HomeDB.Infrastructure.Security
{
    public class PasswordHelper : IPasswordHelper
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        /// <summary>
        /// Genera un hash seguro de una contraseña utilizando PBKDF2 con SHA256 y un salt aleatorio.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KeySize);

            byte[] hashBytes = new byte[SaltSize + KeySize];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
            Buffer.BlockCopy(key, 0, hashBytes, SaltSize, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifica si una contraseña proporcionada coincide con su hash almacenado utilizando PBKDF2.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="storedHash"></param>
        /// <returns></returns>
        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KeySize);

            // Extrae el hash almacenado después de la sal
            byte[] storedKey = new byte[KeySize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedKey, 0, KeySize);

            //Devolver resultado
            return CryptographicOperations.FixedTimeEquals(storedKey, key);
        }

        /// <summary>
        /// Genera un hash seguro de un token de refresco utilizando SHA256.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public string HashRefreshToken(string refreshToken)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(refreshToken);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
