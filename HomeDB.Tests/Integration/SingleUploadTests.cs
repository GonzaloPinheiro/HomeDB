using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace HomeDB.Tests.Integration
{
    //Tests de integración de la subida directa (POST api/files/upload/single): un archivo por petición, límite de
    //20 MiB y validación de magic numbers antes de escribir en disco.
    public sealed class SingleUploadTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string SingleEndpoint = "/api/files/upload/single";
        private const string TestPassword = "Password123!";

        //Límite de appsettings.json (Storage:SingleUploadMaxFileBytes = 20971520)
        private const long ConfiguredLimitBytes = 20L * 1024 * 1024;

        //Firma de PNG para pasar la validación del tipo real del archivo
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        //Constructores
        public SingleUploadTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task UploadSingle_ValidPng_Returns201_AndDownloadMatchesBytes()
        {
            await CreateUserAndLoginAsync("test.single.ok", TestPassword, RolesList.Admin);
            byte[] png = BuildPngBytes(2048, seed: 1);

            HttpResponseMessage response = await PostSingleAsync("photo.png", png);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ApiObjResponse<UploadFileResponseDto>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();
            Assert.True(body!.Result);
            Assert.Equal("photo.png", body.Data!.FileName);
            Assert.Equal(png.Length, body.Data.SizeBytes);
            Assert.Equal("image/png", body.Data.ContentType);

            //El archivo guardado es exactamente el enviado
            HttpResponseMessage download = await Client.GetAsync($"/api/files/{body.Data.Id}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(png, await download.Content.ReadAsByteArrayAsync());
        }

        [Fact]
        public async Task UploadSingle_TextFile_Returns201()
        {
            //Los formatos de texto no tienen magic number: se validan comprobando que el contenido no sea binario
            await CreateUserAndLoginAsync("test.single.text", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("notes.txt", Encoding.UTF8.GetBytes("hola, ñ"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UploadSingle_ExactlyAtTheLimit_Returns201()
        {
            await CreateUserAndLoginAsync("test.single.atlimit", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("limit.png", BuildPngBytes((int)ConfiguredLimitBytes, seed: 2));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UploadSingle_OneByteOverTheLimit_Returns413WithFileTooLargeCode()
        {
            await CreateUserAndLoginAsync("test.single.overlimit", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("over.png", BuildPngBytes((int)ConfiguredLimitBytes + 1, seed: 3));

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.FileTooLarge, body.ErrorCode);

            //No debe quedar nada registrado
            Assert.Empty(await ListRootFilesAsync());
        }

        [Fact]
        public async Task UploadSingle_LimitComesFromConfiguration()
        {
            await CreateUserAndLoginAsync("test.single.configlimit", TestPassword, RolesList.Admin);
            const int oneMegabyte = 1024 * 1024;

            //Con el límite bajado a 1 MiB en la configuración, 1 MiB exacto pasa y 1 byte más no
            using (Factory.LimitSingleUpload(maxFileBytes: oneMegabyte))
            {
                HttpResponseMessage atLimit = await PostSingleAsync("at.png", BuildPngBytes(oneMegabyte, seed: 20));
                Assert.Equal(HttpStatusCode.Created, atLimit.StatusCode);

                HttpResponseMessage overLimit = await PostSingleAsync("over.png", BuildPngBytes(oneMegabyte + 1, seed: 21));
                Assert.Equal(HttpStatusCode.RequestEntityTooLarge, overLimit.StatusCode);
                ApiObjResponse<object>? body = await overLimit.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.FileTooLarge, body!.ErrorCode);

                //Una petición mayor que el límite de la petición completa (1 MB + 1 MiB de margen) la corta el filtro
                HttpResponseMessage hugeBody = await PostSingleAsync("huge.png", BuildPngBytes(3 * oneMegabyte, seed: 22));
                Assert.Equal(HttpStatusCode.RequestEntityTooLarge, hugeBody.StatusCode);
            }

            //Con la configuración restaurada vuelve a valer 20 MB
            HttpResponseMessage restored = await PostSingleAsync("restored.png", BuildPngBytes(3 * oneMegabyte, seed: 23));
            Assert.Equal(HttpStatusCode.Created, restored.StatusCode);
        }

        [Fact]
        public async Task UploadSingle_BodyFarAboveTheRequestLimit_Returns413WithFileTooLargeCode()
        {
            await CreateUserAndLoginAsync("test.single.hugebody", TestPassword, RolesList.Admin);

            //30 MiB: supera incluso el límite de la petición (20 MiB + 1 MiB de margen), así que lo rechaza el
            //filtro RequestSizeGuard por el Content-Length antes de leer el cuerpo.
            //(El [RequestSizeLimit] de Kestrel no se puede probar aquí: TestServer no lo aplica.)
            HttpResponseMessage response = await PostSingleAsync("huge.png", BuildPngBytes(30 * 1024 * 1024, seed: 4));

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.FileTooLarge, body!.ErrorCode);
            Assert.Empty(await ListRootFilesAsync());
        }

        [Fact]
        public async Task UploadSingle_ExecutableRenamedAsPng_Returns400_AndStoresNothing()
        {
            await CreateUserAndLoginAsync("test.single.fake", TestPassword, RolesList.Admin);

            //Cabecera de un ejecutable de Windows (MZ) con extensión .png
            byte[] fake = new byte[2048];
            new Random(5).NextBytes(fake);
            fake[0] = 0x4D;
            fake[1] = 0x5A;

            HttpResponseMessage response = await PostSingleAsync("virus.png", fake);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.InvalidFileExtension, body!.ErrorCode);
            Assert.Empty(await ListRootFilesAsync());
        }

        [Fact]
        public async Task UploadSingle_BinaryRenamedAsText_Returns400()
        {
            await CreateUserAndLoginAsync("test.single.binarytxt", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("fake.txt", BuildPngBytes(2048, seed: 6));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(await ListRootFilesAsync());
        }

        [Fact]
        public async Task UploadSingle_ExtensionOutsideWhitelist_Returns400()
        {
            await CreateUserAndLoginAsync("test.single.exe", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("setup.exe", Encoding.ASCII.GetBytes("MZ"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.InvalidFileExtension, body!.ErrorCode);
        }

        [Fact]
        public async Task UploadSingle_EmptyFile_Returns400()
        {
            await CreateUserAndLoginAsync("test.single.empty", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("empty.txt", Array.Empty<byte>());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.InvalidUploadRequest, body!.ErrorCode);
        }

        [Fact]
        public async Task UploadSingle_FileNameWithPath_KeepsOnlyTheLastSegment()
        {
            await CreateUserAndLoginAsync("test.single.path", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("..\\..\\secret\\evil.png", BuildPngBytes(1024, seed: 7));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ApiObjResponse<UploadFileResponseDto>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();
            Assert.Equal("evil.png", body!.Data!.FileName);
        }

        [Fact]
        public async Task UploadSingle_NonExistentFolder_Returns404()
        {
            await CreateUserAndLoginAsync("test.single.folder", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await PostSingleAsync("a.png", BuildPngBytes(1024, seed: 8), folderId: 999999);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.FolderNotFound, body!.ErrorCode);
        }

        [Fact]
        public async Task UploadSingle_WhenDiskUnavailable_Returns503_AndRecovers()
        {
            await CreateUserAndLoginAsync("test.single.nodisk", TestPassword, RolesList.Admin);
            byte[] png = BuildPngBytes(1024, seed: 9);

            using (Factory.SimulateDiskUnavailable())
            {
                HttpResponseMessage response = await PostSingleAsync("down.png", png);

                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body!.ErrorCode);
            }

            HttpResponseMessage recovered = await PostSingleAsync("down.png", png);
            Assert.Equal(HttpStatusCode.Created, recovered.StatusCode);
        }

        [Fact]
        public async Task UploadSingle_WhenSpaceLimitWouldBeExceeded_Returns507_AndStoresNothing()
        {
            await CreateUserAndLoginAsync("test.single.full", TestPassword, RolesList.Admin);

            using (Factory.LimitStorage(minFreeBytes: long.MaxValue, maxUsedBytes: null))
            {
                HttpResponseMessage response = await PostSingleAsync("full.png", BuildPngBytes(1024, seed: 10));

                Assert.Equal(HttpStatusCode.InsufficientStorage, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationFull, body!.ErrorCode);
            }

            Assert.Empty(await ListRootFilesAsync());
        }

        [Fact]
        public async Task UploadSingle_WithoutAuthentication_Returns401()
        {
            //Sin login previo
            HttpResponseMessage response = await PostSingleAsync("a.png", BuildPngBytes(1024, seed: 11));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #region Helpers
        //Envía un archivo al endpoint de subida directa
        private async Task<HttpResponseMessage> PostSingleAsync(string fileName, byte[] bytes, int? folderId = null)
        {
            using MultipartFormDataContent content = new MultipartFormDataContent();

            ByteArrayContent fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            if (folderId.HasValue)
                content.Add(new StringContent(folderId.Value.ToString()), "folderId");

            return await Client.PostAsync(SingleEndpoint, content);
        }

        //Lista los archivos de la carpeta raíz del usuario actual
        private async Task<List<GetFileItemDto>> ListRootFilesAsync()
        {
            HttpResponseMessage response = await Client.GetAsync("/api/files");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ApiObjResponse<List<GetFileItemDto>>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<List<GetFileItemDto>>>();
            return body!.Data!;
        }

        //Bytes aleatorios con la firma de PNG al inicio
        private static byte[] BuildPngBytes(int size, int seed)
        {
            byte[] buffer = new byte[size];
            new Random(seed).NextBytes(buffer);
            Array.Copy(PngSignature, buffer, Math.Min(PngSignature.Length, buffer.Length));
            return buffer;
        }
        #endregion
    }
}
