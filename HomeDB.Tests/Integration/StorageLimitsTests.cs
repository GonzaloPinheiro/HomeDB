using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    //Tests de integración de los límites de espacio (MinFreeBytes y MaxUsedBytes) en las subidas. Los límites se cambian
    //en caliente con LimitStorage, que los restaura al hacer Dispose.
    public sealed class StorageLimitsTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string InitEndpoint = "/api/files/upload/init";
        private const string ChunkEndpoint = "/api/files/upload/chunk";
        private const string TestPassword = "Password123!";
        private const int ChunkSizeBytes = 1024;

        //Firma de PNG para pasar la validación del tipo real del archivo
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        //Constructores
        public StorageLimitsTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task InitUpload_WhenFreeSpaceMinimumCannotBeKept_Returns507WithStorageLocationFullCode_AndRecovers()
        {
            await CreateUserAndLoginAsync("test.limits.minfree", TestPassword, RolesList.Admin);

            using (Factory.LimitStorage(minFreeBytes: long.MaxValue, maxUsedBytes: null))
            {
                HttpResponseMessage response = await Client.PostAsJsonAsync(InitEndpoint, BuildInitRequest("minfree.png"));

                //Falla al iniciar, antes de que el cliente envíe nada
                Assert.Equal(HttpStatusCode.InsufficientStorage, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.False(body!.Result);
                Assert.Equal(ApiErrorCodes.StorageLocationFull, body.ErrorCode);
                Assert.Contains("not enough free space", body.ErrorMessage);
            }

            //Con los límites de vuelta, la misma petición funciona
            HttpResponseMessage recovered = await Client.PostAsJsonAsync(InitEndpoint, BuildInitRequest("minfree.png"));
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        }

        [Fact]
        public async Task InitUpload_WhenItWouldExceedTheUsageLimit_Returns507WithUsageLimitReason()
        {
            await CreateUserAndLoginAsync("test.limits.maxused", TestPassword, RolesList.Admin);

            //Tope de 1 byte para la ubicación personal: un archivo de 1 KiB no cabe
            using (Factory.LimitStorage(minFreeBytes: 0, maxUsedBytes: 1))
            {
                HttpResponseMessage response = await Client.PostAsJsonAsync(InitEndpoint, BuildInitRequest("maxused.png"));

                Assert.Equal(HttpStatusCode.InsufficientStorage, response.StatusCode);
                ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.Equal(ApiErrorCodes.StorageLocationFull, body!.ErrorCode);
                Assert.Contains("usage limit reached", body.ErrorMessage);
            }
        }

        [Fact]
        public async Task CompleteUpload_WhenSpaceRunsOutAfterStarting_CancelsSessionWithStorageLocationFullCode()
        {
            await CreateUserAndLoginAsync("test.limits.complete", TestPassword, RolesList.Admin);

            //El inicio y el envío del chunk funcionan: en ese momento había espacio
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, BuildInitRequest("complete-full.png"));
            Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);
            ApiObjResponse<UploadInitResponseDto>? initBody = await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();
            Guid sessionId = initBody!.Data!.SessionId;

            using (MultipartFormDataContent chunkContent = BuildChunkContent(sessionId))
            {
                HttpResponseMessage chunkResponse = await Client.PostAsync(ChunkEndpoint, chunkContent);
                Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);
            }

            //Otras subidas ocupan el espacio antes de ensamblar
            UploadStatusResponseDto finalStatus;
            using (Factory.LimitStorage(minFreeBytes: long.MaxValue, maxUsedBytes: null))
            {
                HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
                Assert.Equal(HttpStatusCode.Accepted, completeResponse.StatusCode);

                finalStatus = await WaitForUploadTerminalStatusAsync(Client, sessionId);
            }

            //La sesión no se queda atascada en Processing: termina cancelada con un código claro
            Assert.Equal(UploadSessionStatus.Cancelled, finalStatus.Status);
            Assert.Equal(ApiErrorCodes.StorageLocationFull, finalStatus.ErrorCode);
        }

        //Helpers
        private static UploadInitRequestDto BuildInitRequest(string fileName)
        {
            return new UploadInitRequestDto
            {
                FileName = fileName,
                TotalSizeBytes = ChunkSizeBytes,
                TotalChunks = 1,
                FolderId = null
            };
        }

        private static MultipartFormDataContent BuildChunkContent(Guid sessionId)
        {
            byte[] chunkBytes = new byte[ChunkSizeBytes];
            new Random(7).NextBytes(chunkBytes);
            Array.Copy(PngSignature, chunkBytes, PngSignature.Length);

            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(sessionId.ToString()), "sessionId" },
                { new StringContent("1"), "chunkNumber" }
            };

            ByteArrayContent chunkContent = new ByteArrayContent(chunkBytes);
            chunkContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(chunkContent, "chunk", "chunk.bin");

            return content;
        }
    }
}
