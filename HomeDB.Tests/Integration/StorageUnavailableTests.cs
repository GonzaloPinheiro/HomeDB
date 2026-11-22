using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    //Tests de integración con el disco de almacenamiento caído. Se simula borrando el fichero marcador del disco
    //de pruebas (lo que ocurre en la práctica cuando un disco no está montado) y restaurándolo al terminar.
    //La colección comparte una sola API entre todos los tests y los ejecuta en serie, así que es seguro mientras
    //cada test restaure el marcador (el valor que devuelve SimulateDiskUnavailable lo hace al hacer Dispose).
    public sealed class StorageUnavailableTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string InitEndpoint = "/api/files/upload/init";
        private const string ChunkEndpoint = "/api/files/upload/chunk";
        private const string TestPassword = "Password123!";
        private const int ChunkSizeBytes = 1024;

        //Firma de PNG para pasar la validación del tipo real del archivo
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        //Constructores
        public StorageUnavailableTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task InitUpload_WhenDiskUnavailable_Returns503WithStorageLocationUnavailableCode_AndRecovers()
        {
            await CreateUserAndLoginAsync("test.storage.init", TestPassword, RolesList.Admin);
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "no-disk.png",
                TotalSizeBytes = ChunkSizeBytes,
                TotalChunks = 1,
                FolderId = null
            };

            using (Factory.SimulateDiskUnavailable())
            {
                HttpResponseMessage response = await Client.PostAsJsonAsync(InitEndpoint, initRequest);

                //Falla rápido, antes de que el cliente envíe ningún chunk
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.NotNull(body);
                Assert.False(body!.Result);
                Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body.ErrorCode);
            }

            //Con el disco de vuelta, la misma petición funciona
            HttpResponseMessage recovered = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        }

        [Fact]
        public async Task UploadChunk_WhenDiskBecomesUnavailable_Returns503()
        {
            await CreateUserAndLoginAsync("test.storage.chunk", TestPassword, RolesList.Admin);
            Guid sessionId = await InitSingleChunkUploadAsync("chunk-down.png");

            using (Factory.SimulateDiskUnavailable())
            {
                using MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, 1, BuildPngBytes(seed: 1));
                HttpResponseMessage response = await Client.PostAsync(ChunkEndpoint, chunkContent);

                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body!.ErrorCode);
            }
        }

        [Fact]
        public async Task CompleteUpload_WhenDiskGoesDownBeforeAssembling_CancelsSessionWithStorageLocationUnavailableCode()
        {
            await CreateUserAndLoginAsync("test.storage.complete", TestPassword, RolesList.Admin);
            Guid sessionId = await InitSingleChunkUploadAsync("complete-down.png");

            using MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, 1, BuildPngBytes(seed: 2));
            HttpResponseMessage chunkResponse = await Client.PostAsync(ChunkEndpoint, chunkContent);
            Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);

            UploadStatusResponseDto finalStatus;
            using (Factory.SimulateDiskUnavailable())
            {
                //El complete solo encola el ensamblado: el fallo ocurre en el worker en segundo plano
                HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
                Assert.Equal(HttpStatusCode.Accepted, completeResponse.StatusCode);

                finalStatus = await WaitForUploadTerminalStatusAsync(Client, sessionId);
            }

            //La sesión no se queda atascada en Processing: termina cancelada con un código de error claro
            Assert.Equal(UploadSessionStatus.Cancelled, finalStatus.Status);
            Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, finalStatus.ErrorCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/stream")]
        public async Task Download_WhenDiskUnavailable_Returns503_NotFileNotFound_AndRecovers(string suffix)
        {
            await CreateUserAndLoginAsync("test.storage.download" + suffix.Replace("/", "."), TestPassword, RolesList.Admin);
            int fileId = await UploadSmallPngAsync("download-down.png");

            using (Factory.SimulateDiskUnavailable())
            {
                HttpResponseMessage response = await Client.GetAsync($"/api/files/{fileId}{suffix}");

                //El caso que el módulo quiere evitar: un disco ausente no debe parecer un "archivo no encontrado"
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body!.ErrorCode);
            }

            HttpResponseMessage recovered = await Client.GetAsync($"/api/files/{fileId}{suffix}");
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        }

        [Fact]
        public async Task DeleteFile_WhenDiskUnavailable_Returns503_AndKeepsTheFileAndItsRecord()
        {
            await CreateUserAndLoginAsync("test.storage.delete", TestPassword, RolesList.Admin);
            int fileId = await UploadSmallPngAsync("delete-down.png");

            using (Factory.SimulateDiskUnavailable())
            {
                HttpResponseMessage response = await Client.DeleteAsync($"/api/files/{fileId}");

                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body!.ErrorCode);
            }

            //Sin la comprobación, el borrado habría "funcionado" quitando el registro de la base de datos y dejando
            //el fichero huérfano. Con el disco de vuelta, el archivo y su registro siguen ahí.
            HttpResponseMessage download = await Client.GetAsync($"/api/files/{fileId}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);

            HttpResponseMessage delete = await Client.DeleteAsync($"/api/files/{fileId}");
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        }

        #region Helpers
        //Inicia una subida de un solo chunk y devuelve su sessionId
        private async Task<Guid> InitSingleChunkUploadAsync(string fileName)
        {
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = fileName,
                TotalSizeBytes = ChunkSizeBytes,
                TotalChunks = 1,
                FolderId = null
            };

            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            return initBody!.Data!.SessionId;
        }

        //Sube un archivo de un solo chunk por el flujo completo y devuelve el id del archivo resultante
        private async Task<int> UploadSmallPngAsync(string fileName)
        {
            Guid sessionId = await InitSingleChunkUploadAsync(fileName);

            using (MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, 1, BuildPngBytes(seed: 3)))
            {
                HttpResponseMessage chunkResponse = await Client.PostAsync(ChunkEndpoint, chunkContent);
                Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);
            }

            HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.Accepted, completeResponse.StatusCode);

            UploadStatusResponseDto finalStatus = await WaitForUploadTerminalStatusAsync(Client, sessionId);
            Assert.Equal(UploadSessionStatus.Completed, finalStatus.Status);

            return finalStatus.File!.Id;
        }

        private static byte[] BuildPngBytes(int seed)
        {
            byte[] buffer = new byte[ChunkSizeBytes];
            new Random(seed).NextBytes(buffer);
            Array.Copy(PngSignature, buffer, PngSignature.Length);
            return buffer;
        }

        private static MultipartFormDataContent BuildChunkContent(Guid sessionId, int chunkNumber, byte[] chunkBytes)
        {
            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(sessionId.ToString()), "sessionId" },
                { new StringContent(chunkNumber.ToString()), "chunkNumber" }
            };

            ByteArrayContent chunkContent = new ByteArrayContent(chunkBytes);
            chunkContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(chunkContent, "chunk", "chunk.bin");

            return content;
        }
        #endregion
    }
}
