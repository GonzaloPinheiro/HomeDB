using System.IO.Compression;
using System.Text;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Services;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Tests unitarios de la validación de magic numbers. Cubren cada extensión de la whitelist para que
    //ninguna quede aceptada en AllowedExtensions pero rechazada siempre por el validador.
    public sealed class MimeDetectiveFileTypeValidatorTests
    {
        private readonly MimeDetectiveFileTypeValidator _validator = new MimeDetectiveFileTypeValidator();

        //Cabecera con los bytes indicados seguidos de relleno aleatorio, como en un archivo real.
        private static byte[] Header(params byte[] signature)
        {
            byte[] header = new byte[64];
            new Random(1).NextBytes(header);
            Array.Copy(signature, header, signature.Length);
            return header;
        }

        private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

        //Cabecera de un OOXML real: un zip con las entradas propias de Word o Excel.
        private static byte[] BuildOoxmlHeader(string mainEntry)
        {
            using MemoryStream stream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (string entryName in new[] { "[Content_Types].xml", mainEntry })
                {
                    ZipArchiveEntry entry = zip.CreateEntry(entryName);
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write("<xml/>");
                }
            }

            byte[] bytes = stream.ToArray();
            return bytes.AsSpan(0, Math.Min(bytes.Length, FileHeader.Length)).ToArray();
        }

        public static IEnumerable<object[]> ValidBinaryHeaders()
        {
            yield return new object[] { "a.png", Header(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A) };
            yield return new object[] { "a.jpg", Header(0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01) };
            yield return new object[] { "a.jpeg", Header(0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x10, 0x45, 0x78, 0x69, 0x66, 0x00, 0x00) };
            yield return new object[] { "a.gif", Header(Ascii("GIF89a")) };
            yield return new object[] { "a.webp", Header(0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20) };
            yield return new object[] { "a.mp4", Header(0, 0, 0, 0x18, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D, 0, 0, 2, 0) };
            yield return new object[] { "a.mov", Header(0, 0, 0, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20, 0, 0, 0, 0) };
            yield return new object[] { "a.wav", Header(0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20) };
            yield return new object[] { "a.flac", Header(Ascii("fLaC")) };
            yield return new object[] { "a.pdf", Header(Ascii("%PDF-1.7\n")) };
            yield return new object[] { "a.zip", Header(0x50, 0x4B, 0x03, 0x04, 0x14, 0, 0, 0, 8, 0, 0, 0, 0, 0) };
            yield return new object[] { "a.7z", Header(0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, 0, 4) };
            yield return new object[] { "a.rar", Header(0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0) };
            yield return new object[] { "a.mp3", Header(Ascii("ID3")) };
            yield return new object[] { "b.mp3", Header(0xFF, 0xFB, 0x90, 0x44) };
            yield return new object[] { "a.docx", BuildOoxmlHeader("word/document.xml") };
            yield return new object[] { "a.xlsx", BuildOoxmlHeader("xl/workbook.xml") };
            yield return new object[] { "A.PNG", Header(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A) };
        }

        [Theory]
        [MemberData(nameof(ValidBinaryHeaders))]
        public void IsValid_BinaryWithMatchingSignature_ReturnsTrue(string fileName, byte[] header)
        {
            Assert.True(_validator.IsValid(fileName, header));
        }

        [Theory]
        [InlineData("a.txt", "hello world")]
        [InlineData("a.md", "# Titulo\n\ntexto con acentos: áéíóú ñ")]
        [InlineData("a.csv", "a,b,c\n1,2,3\n")]
        [InlineData("a.json", "{\"a\":1}")]
        [InlineData("a.xml", "<root/>")]
        [InlineData("a.yaml", "key: value\n")]
        [InlineData("a.yml", "key: value\n")]
        public void IsValid_TextFormatsWithTextContent_ReturnsTrue(string fileName, string content)
        {
            Assert.True(_validator.IsValid(fileName, Encoding.UTF8.GetBytes(content)));
        }

        [Fact]
        public void IsValid_ShortTextFileWithoutPadding_ReturnsTrue()
        {
            //Con la cabecera exacta (sin relleno de ceros) un archivo de texto corto es válido.
            Assert.True(_validator.IsValid("a.txt", Ascii("ok")));
        }

        [Fact]
        public void IsValid_EmptyTextFile_ReturnsTrue()
        {
            Assert.True(_validator.IsValid("a.txt", Array.Empty<byte>()));
        }

        [Theory]
        [InlineData("a.png")]
        [InlineData("a.pdf")]
        [InlineData("a.zip")]
        [InlineData("a.mp3")]
        [InlineData("a.docx")]
        public void IsValid_EmptyBinary_ReturnsFalse(string fileName)
        {
            Assert.False(_validator.IsValid(fileName, Array.Empty<byte>()));
        }

        [Theory]
        [InlineData("a.png")]
        [InlineData("a.jpg")]
        [InlineData("a.pdf")]
        [InlineData("a.zip")]
        [InlineData("a.mp4")]
        [InlineData("a.mp3")]
        [InlineData("a.docx")]
        public void IsValid_ExecutableRenamed_ReturnsFalse(string fileName)
        {
            //Cabecera de un ejecutable de Windows (MZ) con otra extensión.
            Assert.False(_validator.IsValid(fileName, Header(0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00)));
        }

        [Theory]
        [InlineData("a.txt")]
        [InlineData("a.md")]
        [InlineData("a.csv")]
        [InlineData("a.json")]
        [InlineData("a.yaml")]
        public void IsValid_BinaryRenamedAsText_ReturnsFalse(string fileName)
        {
            //Un PNG con extensión de texto contiene bytes NUL.
            byte[] png = new byte[64];
            Array.Copy(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D }, png, 12);
            Assert.False(_validator.IsValid(fileName, png));
        }

        [Fact]
        public void IsValid_PngSignatureWithPdfExtension_ReturnsFalse()
        {
            Assert.False(_validator.IsValid("a.pdf", Header(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)));
        }

        [Fact]
        public void IsValid_PlainZipWithDocxExtension_IsAcceptedBecauseHeaderCannotTellThemApart()
        {
            //Limitación conocida y aceptada: un zip cualquiera renombrado a .docx pasa la comprobación de cabecera.
            Assert.True(_validator.IsValid("a.docx", Header(0x50, 0x4B, 0x03, 0x04, 0x14, 0, 0, 0, 8, 0, 0, 0, 0, 0)));
        }

        [Theory]
        [InlineData("a.exe")]
        [InlineData("a.sh")]
        [InlineData("noextension")]
        public void IsValid_ExtensionOutsideWhitelist_ReturnsFalse(string fileName)
        {
            Assert.False(_validator.IsValid(fileName, Ascii("text")));
        }

        [Fact]
        public void IsValid_EveryWhitelistedExtensionHasAValidCaseCovered()
        {
            //Si se añade una extensión a la whitelist sin decidir cómo se valida, este test avisa.
            HashSet<string> covered = new(StringComparer.OrdinalIgnoreCase);
            foreach (object[] row in ValidBinaryHeaders())
                covered.Add(Path.GetExtension((string)row[0]));
            foreach (string text in new[] { ".txt", ".md", ".csv", ".json", ".xml", ".yaml", ".yml" })
                covered.Add(text);

            foreach (string extension in AllowedExtensions.Whitelist)
                Assert.Contains(extension, covered);
        }

        [Fact]
        public async Task FileHeader_ReadAsync_ReturnsExactLengthOfShortStream()
        {
            using MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
            byte[] header = await FileHeader.ReadAsync(stream, CancellationToken.None);
            Assert.Equal(new byte[] { 1, 2, 3 }, header);
        }

        [Fact]
        public async Task FileHeader_ReadAsync_CapsAtHeaderLength()
        {
            using MemoryStream stream = new MemoryStream(new byte[FileHeader.Length * 2]);
            byte[] header = await FileHeader.ReadAsync(stream, CancellationToken.None);
            Assert.Equal(FileHeader.Length, header.Length);
        }
    }
}
