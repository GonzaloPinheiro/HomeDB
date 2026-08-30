using HomeDB.Infrastructure.Security;

namespace HomeDB.Tests.Unit.Security
{
    //Tests unitarios puros (sin DB ni contenedores) de PasswordHelper: hasheo/verificación de
    //contraseñas con PBKDF2 y hasheo de refresh tokens con SHA256.
    public sealed class PasswordHelperTests
    {
        private readonly PasswordHelper _passwordHelper = new PasswordHelper();

        #region HashPassword

        [Fact]
        public void HashPassword_ReturnsValidBase64StringOfExpectedLength()
        {
            //Hashear una contraseña cualquiera
            string hash = _passwordHelper.HashPassword("Password123!");

            //El hash es salt(16 bytes) + key(32 bytes) = 48 bytes, que en Base64 (48 es múltiplo de 3)
            //se codifican como 64 caracteres sin padding.
            Assert.Equal(64, hash.Length);
            Assert.DoesNotContain('=', hash);

            //Debe ser Base64 válido y decodificar exactamente a 48 bytes
            byte[] decoded = Convert.FromBase64String(hash);
            Assert.Equal(48, decoded.Length);
        }

        [Fact]
        public void HashPassword_CalledTwiceWithSamePassword_ProducesDifferentHashes()
        {
            //El salt aleatorio hace que el mismo password nunca genere el mismo hash dos veces
            const string password = "Password123!";

            string firstHash = _passwordHelper.HashPassword(password);
            string secondHash = _passwordHelper.HashPassword(password);

            Assert.NotEqual(firstHash, secondHash);
        }

        [Fact]
        public void HashPassword_WithEmptyPassword_StillProducesAValidHash()
        {
            //No hay validación de contraseña vacía a este nivel, PasswordHelper solo hashea lo que recibe
            string hash = _passwordHelper.HashPassword(string.Empty);

            Assert.Equal(64, hash.Length);
        }

        #endregion

        #region VerifyPassword

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
        {
            const string password = "Password123!";
            string hash = _passwordHelper.HashPassword(password);

            Assert.True(_passwordHelper.VerifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
        {
            string hash = _passwordHelper.HashPassword("Password123!");

            Assert.False(_passwordHelper.VerifyPassword("WrongPassword!", hash));
        }

        [Fact]
        public void VerifyPassword_IsCaseSensitive()
        {
            string hash = _passwordHelper.HashPassword("Password123!");

            Assert.False(_passwordHelper.VerifyPassword("password123!", hash));
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_RoundTripsCorrectly()
        {
            //Un hash de contraseña vacía solo debe verificar correctamente contra una contraseña vacía
            string hash = _passwordHelper.HashPassword(string.Empty);

            Assert.True(_passwordHelper.VerifyPassword(string.Empty, hash));
            Assert.False(_passwordHelper.VerifyPassword("notEmpty", hash));
        }

        #endregion

        #region HashRefreshToken

        [Fact]
        public void HashRefreshToken_WithSameInput_IsDeterministic()
        {
            //A diferencia de HashPassword, HashRefreshToken no usa salt: debe ser determinista
            const string token = "some-refresh-token-value";

            string firstHash = _passwordHelper.HashRefreshToken(token);
            string secondHash = _passwordHelper.HashRefreshToken(token);

            Assert.Equal(firstHash, secondHash);
        }

        [Fact]
        public void HashRefreshToken_WithDifferentInputs_ProducesDifferentHashes()
        {
            string firstHash = _passwordHelper.HashRefreshToken("token-a");
            string secondHash = _passwordHelper.HashRefreshToken("token-b");

            Assert.NotEqual(firstHash, secondHash);
        }

        [Fact]
        public void HashRefreshToken_ReturnsValidBase64Sha256Hash()
        {
            string hash = _passwordHelper.HashRefreshToken("some-refresh-token-value");

            //SHA256 produce 32 bytes, que en Base64 se codifican como 44 caracteres (con un '=' de padding)
            Assert.Equal(44, hash.Length);

            byte[] decoded = Convert.FromBase64String(hash);
            Assert.Equal(32, decoded.Length);
        }

        #endregion
    }
}
