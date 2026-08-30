using HomeDB.Domain.Common;

namespace HomeDB.Tests.Unit.Domain
{
    //Tests unitarios de la whitelist de extensiones permitidas para subida de archivos.
    public sealed class AllowedExtensionsTests
    {
        [Theory]
        [InlineData(".jpg")]
        [InlineData(".png")]
        [InlineData(".mp4")]
        [InlineData(".mp3")]
        [InlineData(".pdf")]
        [InlineData(".docx")]
        [InlineData(".zip")]
        [InlineData(".json")]
        public void Whitelist_ContainsKnownAllowedExtensions(string extension)
        {
            Assert.Contains(extension, AllowedExtensions.Whitelist);
        }

        [Theory]
        [InlineData(".exe")]
        [InlineData(".sh")]
        [InlineData(".bat")]
        [InlineData(".dll")]
        public void Whitelist_DoesNotContainDangerousOrUnlistedExtensions(string extension)
        {
            Assert.DoesNotContain(extension, AllowedExtensions.Whitelist);
        }

        [Theory]
        [InlineData(".PNG")]
        [InlineData(".Png")]
        [InlineData(".JPG")]
        public void Whitelist_LookupIsCaseInsensitive(string extension)
        {
            //El HashSet se construye con StringComparer.OrdinalIgnoreCase, por lo que la comprobación
            //no debe depender de si la extensión llega en mayúsculas o minúsculas.
            Assert.Contains(extension, AllowedExtensions.Whitelist);
        }
    }
}
