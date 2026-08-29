using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    public sealed class UploadTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string InitEndpoint = "/api/files/upload/init";
        private const string ChunkEndpoint = "/api/files/upload/chunk";
        private const string TestPassword = "Password123!";

        //Tamaño de chunk arbitrario para el test.
        private const int ChunkSizeBytes = 1024;

        //Firma de PNG
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };


        //Constructores
        public UploadTests(HomeDbApiFactory factory) : base(factory)
        {
        }


        [Fact]
        public async Task Upload_FullChunkedFlow_CompletesSuccessfully()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.fullflow";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear 3 chunks de datos aleatorios.
            const int totalChunks = 3;
            byte[][] chunks = new byte[totalChunks][];
            for (int i = 0; i < totalChunks; i++)
            {
                chunks[i] = BuildRandomBytes(ChunkSizeBytes, seed: i, withPngSignature: i == 0);
            }

            //Calcular el tamaño total del archivo sumando los tamaños de los chunks
            long totalSizeBytes = chunks.Sum(c => (long)c.Length);

            //Dto de inicialización de la subida
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "integration-test-file.png",
                TotalSizeBytes = totalSizeBytes,
                TotalChunks = totalChunks,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);

            //Comrpobar que la respuesta es OK
            Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);

            //Deserializar la respuesta y comprobar que contiene un result y data
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();
            Assert.NotNull(initBody);
            Assert.True(initBody!.Result);
            Assert.NotNull(initBody.Data);

            //Obtener el sessionId de la subida del archivo
            Guid sessionId = initBody.Data!.SessionId;

            //Enviar cada chunk al endpoint de subida de chunks.
            for (int i = 0; i < totalChunks; i++)
            {
                //Construir el contenido del chunk con el sessionId, el número de chunk y los bytes del chunk
                using MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, i + 1, chunks[i]);

                //Hacer la llamada al endpoint de subida de chunks y comprobar que la respuesta es OK
                HttpResponseMessage chunkResponse = await Client.PostAsync(ChunkEndpoint, chunkContent);
                Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);
            }

            //Obtener el estado de la subida para verificar que todos los chunks fueron recibidos
            HttpResponseMessage statusResponse = await Client.GetAsync($"/api/files/upload/{sessionId}/status");
            Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

            //Comprobar que la respuesta contiene un result y data con la lista de chunks recibidos
            ApiObjResponse<UploadStatusResponseDto>? statusBody =
                await statusResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadStatusResponseDto>>();
            Assert.NotNull(statusBody);
            Assert.Equal(totalChunks, statusBody!.Data!.ReceivedChunks.Count);

            //Hacer la llamada al endpoint de completar la subida y comprobar que la respuesta es OK
            HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

            //Comprobar que la respuesta contiene un result y data con la información del archivo subido
            ApiObjResponse<UploadFileResponseDto>? completeBody =
                await completeResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();
            Assert.NotNull(completeBody);
            Assert.True(completeBody!.Result);
            Assert.Equal("integration-test-file.png", completeBody.Data!.FileName);
            Assert.Equal(totalSizeBytes, completeBody.Data.SizeBytes);
        }

        [Fact]
        public async Task Upload_CompleteCalledTwice_SecondCallReturnsAlreadyCompletedMessage()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.completetwice";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear un solo chunk de datos aleatorios con la firma de PNG
            byte[] singleChunk = BuildRandomBytes(ChunkSizeBytes, seed: 42, withPngSignature: true);

            //Crear el DTO de inicialización de la subida con un solo chunk
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "double-complete.png",
                TotalSizeBytes = singleChunk.Length,
                TotalChunks = 1,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            //Obtener el sessionId de la respuesta de inicialización
            Guid sessionId = initBody!.Data!.SessionId;

            //Enviar el único chunk
            using (MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, 1, singleChunk))
            {
                await Client.PostAsync(ChunkEndpoint, chunkContent);
            }

            //Hacer la primera llamada al endpoint de completar la subida y comprobar que es OK
            HttpResponseMessage firstComplete = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.OK, firstComplete.StatusCode);

            //Hacer la segunda llamada al endpoint de completar la subida y comprobar que es OK
            HttpResponseMessage secondComplete = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);

            //Comprobar que la respuesta sigue siendo OK
            Assert.Equal(HttpStatusCode.OK, secondComplete.StatusCode);

            //Deserializar la respuesta y comprobar que contiene un result y data con el mensaje de ya completado
            ApiObjResponse<string>? secondBody =
                await secondComplete.Content.ReadFromJsonAsync<ApiObjResponse<string>>();
            Assert.NotNull(secondBody);
            Assert.True(secondBody!.Result);
        }

        [Fact]
        public async Task Upload_CompleteWithMissingChunks_ReturnsBadRequestIncomplete()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.incomplete";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear el DTO de inicialización de la subida con 2 chunks, pero solo se enviará 1
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "incomplete.bin",
                TotalSizeBytes = ChunkSizeBytes * 2,
                TotalChunks = 2,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            //Obtener el sessionId de la respuesta de inicialización
            Guid sessionId = initBody!.Data!.SessionId;

            //Solo se envía uno de los dos chunks requeridos (número 1), dejando el otro sin enviar
            using (MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, 1, BuildRandomBytes(ChunkSizeBytes, seed: 1)))
            {
                await Client.PostAsync(ChunkEndpoint, chunkContent);
            }

            //Hacer la llamada al endpoint de completar la subida y comprobar que devuelve BadRequest por chunks faltantes
            HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);

            //Deserializar la respuesta y comprobar que contiene un result false y el error code UploadIncomplete
            ApiObjResponse<object>? body =
                await completeResponse.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.UploadIncomplete, body.ErrorCode);
        }

        [Fact]
        public async Task Upload_WithNonExistentSession_ReturnsNotFound()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.nosession";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Generar un sessionId aleatorio que no existe en el sistema
            Guid fakeSessionId = Guid.NewGuid();

            //Hacer la llamada al endpoint de estado de la subida con un sessionId inexistente y comprobar que devuelve NotFound
            HttpResponseMessage response = await Client.GetAsync($"/api/files/upload/{fakeSessionId}/status");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            //Deserializar la respuesta y comprobar que contiene un result false y el error code UploadSessionNotFound
            ApiObjResponse<object>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.Equal(ApiErrorCodes.UploadSessionNotFound, body!.ErrorCode);
        }

        [Fact]
        public async Task Upload_WithChunkNumberOutOfRange_ReturnsBadRequestWithInvalidChunkNumberCode()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.invalidchunknumber";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear el DTO de inicialización de la subida con 2 chunks esperados
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "invalid-chunk-number.bin",
                TotalSizeBytes = ChunkSizeBytes * 2,
                TotalChunks = 2,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            //Obtener el sessionId de la respuesta de inicialización
            Guid sessionId = initBody!.Data!.SessionId;

            //Intentar enviar un chunk con chunkNumber 0, por debajo del rango válido (que empieza en 1)
            using (MultipartFormDataContent belowRangeContent = BuildChunkContent(sessionId, 0, BuildRandomBytes(ChunkSizeBytes, seed: 1)))
            {
                HttpResponseMessage belowRangeResponse = await Client.PostAsync(ChunkEndpoint, belowRangeContent);

                //Comprobar que la respuesta es BadRequest por chunkNumber fuera de rango
                Assert.Equal(HttpStatusCode.BadRequest, belowRangeResponse.StatusCode);

                //Deserializar la respuesta y comprobar que contiene un result false y el error code InvalidChunkNumber
                ApiObjResponse<object>? belowRangeBody =
                    await belowRangeResponse.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.NotNull(belowRangeBody);
                Assert.False(belowRangeBody!.Result);
                Assert.Equal(ApiErrorCodes.InvalidChunkNumber, belowRangeBody.ErrorCode);
            }

            //Intentar enviar un chunk con chunkNumber 3, por encima del rango válido (TotalChunks es 2)
            using (MultipartFormDataContent aboveRangeContent = BuildChunkContent(sessionId, 3, BuildRandomBytes(ChunkSizeBytes, seed: 2)))
            {
                HttpResponseMessage aboveRangeResponse = await Client.PostAsync(ChunkEndpoint, aboveRangeContent);

                //Comprobar que la respuesta es BadRequest por chunkNumber fuera de rango
                Assert.Equal(HttpStatusCode.BadRequest, aboveRangeResponse.StatusCode);

                //Deserializar la respuesta y comprobar que contiene un result false y el error code InvalidChunkNumber
                ApiObjResponse<object>? aboveRangeBody =
                    await aboveRangeResponse.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
                Assert.NotNull(aboveRangeBody);
                Assert.False(aboveRangeBody!.Result);
                Assert.Equal(ApiErrorCodes.InvalidChunkNumber, aboveRangeBody.ErrorCode);
            }
        }

        [Fact]
        public async Task Upload_ReceivingSameChunkNumberTwice_OverwritesContentWithoutDuplicatingStatus()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.samechunktwice";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear dos versiones distintas del mismo chunk (mismo tamaño, contenido distinto), ambas
            //con la firma de PNG porque es el único chunk de la sesión
            byte[] firstVersion = BuildRandomBytes(ChunkSizeBytes, seed: 10, withPngSignature: true);
            byte[] secondVersion = BuildRandomBytes(ChunkSizeBytes, seed: 20, withPngSignature: true);

            //Crear el DTO de inicialización de la subida con un solo chunk
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "same-chunk-twice.png",
                TotalSizeBytes = ChunkSizeBytes,
                TotalChunks = 1,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            //Obtener el sessionId de la respuesta de inicialización
            Guid sessionId = initBody!.Data!.SessionId;

            //Enviar la primera versión del chunk número 1
            using (MultipartFormDataContent firstContent = BuildChunkContent(sessionId, 1, firstVersion))
            {
                HttpResponseMessage firstResponse = await Client.PostAsync(ChunkEndpoint, firstContent);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            //Reenviar el mismo número de chunk con contenido distinto: ReceiveChunkAsync no lo rechaza,
            //sobrescribe el archivo en disco y no vuelve a insertar el registro en UploadChunk
            using (MultipartFormDataContent secondContent = BuildChunkContent(sessionId, 1, secondVersion))
            {
                HttpResponseMessage secondResponse = await Client.PostAsync(ChunkEndpoint, secondContent);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            }

            //Comprobar que el estado de la subida sigue mostrando un solo chunk recibido (no se duplica el registro)
            HttpResponseMessage statusResponse = await Client.GetAsync($"/api/files/upload/{sessionId}/status");
            ApiObjResponse<UploadStatusResponseDto>? statusBody =
                await statusResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadStatusResponseDto>>();
            Assert.NotNull(statusBody);
            Assert.Single(statusBody!.Data!.ReceivedChunks);

            //Completar la subida
            HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

            //Obtener el id del archivo subido para poder descargarlo
            ApiObjResponse<UploadFileResponseDto>? completeBody =
                await completeResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();
            Assert.NotNull(completeBody);

            //Descargar el archivo ensamblado y comprobar que su contenido es el de la segunda versión del
            //chunk, confirmando que reenviar un chunk sobrescribe en vez de ignorar o acumular
            HttpResponseMessage downloadResponse = await Client.GetAsync($"/api/files/{completeBody!.Data!.Id}");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

            byte[] downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.Equal(secondVersion, downloadedBytes);
        }

        [Fact]
        public async Task Upload_AfterCompletion_DownloadedFileMatchesOriginalBytesExactly()
        {
            //Crear usuario y hacer login
            const string username = "test.upload.bytematch";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Crear 3 chunks de datos aleatorios. El primero lleva la firma de PNG porque
            //CompleteUploadAsync valida el tipo real del archivo ensamblado contra su extensión.
            const int totalChunks = 3;
            byte[][] chunks = new byte[totalChunks][];
            for (int i = 0; i < totalChunks; i++)
            {
                chunks[i] = BuildRandomBytes(ChunkSizeBytes, seed: 100 + i, withPngSignature: i == 0);
            }

            //Calcular el tamaño total del archivo sumando los tamaños de los chunks
            long totalSizeBytes = chunks.Sum(c => (long)c.Length);

            //Dto de inicialización de la subida
            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = "byte-match.png",
                TotalSizeBytes = totalSizeBytes,
                TotalChunks = totalChunks,
                FolderId = null
            };

            //Hacer la llamada al endpoint de inicialización de la subida
            HttpResponseMessage initResponse = await Client.PostAsJsonAsync(InitEndpoint, initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();

            //Obtener el sessionId de la respuesta de inicialización
            Guid sessionId = initBody!.Data!.SessionId;

            //Enviar cada chunk en orden, con números de chunk 1-based
            for (int i = 0; i < totalChunks; i++)
            {
                using MultipartFormDataContent chunkContent = BuildChunkContent(sessionId, i + 1, chunks[i]);
                HttpResponseMessage chunkResponse = await Client.PostAsync(ChunkEndpoint, chunkContent);
                Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);
            }

            //Completar la subida
            HttpResponseMessage completeResponse = await Client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

            //Obtener el id del archivo subido para poder descargarlo
            ApiObjResponse<UploadFileResponseDto>? completeBody =
                await completeResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();
            Assert.NotNull(completeBody);

            //Descargar el archivo ensamblado a través del endpoint de descarga de FilesController
            HttpResponseMessage downloadResponse = await Client.GetAsync($"/api/files/{completeBody!.Data!.Id}");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

            //Comprobar que el contenido descargado es exactamente la concatenación de los chunks
            //originales, byte a byte, verificando que el ensamblado en CompleteUploadAsync no corrompe datos
            byte[] downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            byte[] expectedBytes = chunks.SelectMany(c => c).ToArray();
            Assert.Equal(expectedBytes, downloadedBytes);
        }

        #region Helpers
        //Genera un arreglo de bytes aleatorios de tamaño especificado, con una semilla para reproducibilidad.
        private static byte[] BuildRandomBytes(int size, int seed, bool withPngSignature = false)
        {
            byte[] buffer = new byte[size];
            Random random = new Random(seed);
            random.NextBytes(buffer);

            //Sobrescribir el inicio del buffer con la firma de PNG si se pide.(Para pasar el mimetype)
            if (withPngSignature)
            {
                Array.Copy(PngSignature, buffer, Math.Min(PngSignature.Length, buffer.Length));
            }

            return buffer;
        }

        //Construye el contenido de un chunk para enviarlo al endpoint de subida de chunks.
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