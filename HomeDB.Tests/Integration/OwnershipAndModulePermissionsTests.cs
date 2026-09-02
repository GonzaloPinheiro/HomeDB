using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Auth;
using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    //Tests de ownership (un usuario no puede acceder/modificar recursos de otro) y de permisos
    //por módulo (RequireModule bloquea módulos no habilitados salvo para el rol Admin).
    public sealed class OwnershipAndModulePermissionsTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string TestPassword = "Password123!";
        private const int ChunkSizeBytes = 1024;
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        //Constructores
        public OwnershipAndModulePermissionsTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        #region Ownership: Archivos

        [Fact]
        public async Task DownloadFile_OwnedByAnotherUser_ReturnsNotFoundWithFileNotFoundCode()
        {
            //Crear dos usuarios Admin (el rol Admin evita las restricciones de módulos, no es lo que se está probando aquí)
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.file.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.file.other", RolesList.Admin);

            //El dueño sube un archivo
            int fileId = await UploadSingleFileAsync(ownerClient, "owner-file.png");

            //El otro usuario intenta descargar el archivo del dueño
            HttpResponseMessage response = await otherClient.GetAsync($"/api/files/{fileId}");

            //Debe devolver 404 con el código FileNotFound, nunca el contenido del archivo ajeno
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.FileNotFound, body.ErrorCode);
        }

        [Fact]
        public async Task UpdateFile_OwnedByAnotherUser_ReturnsNotFoundWithFileNotFoundCode()
        {
            //Crear dos usuarios Admin
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.file.update.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.file.update.other", RolesList.Admin);

            //El dueño sube un archivo
            int fileId = await UploadSingleFileAsync(ownerClient, "owner-file-update.png");

            //El otro usuario intenta renombrar el archivo del dueño
            UpdateFileRequestDto updateDto = new UpdateFileRequestDto { NewFileName = "hijacked.png" };
            HttpResponseMessage response = await otherClient.PatchAsJsonAsync($"/api/files/{fileId}", updateDto);

            //Debe devolver 404 con el código FileNotFound, sin modificar el archivo ajeno
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.FileNotFound, body.ErrorCode);
        }

        [Fact]
        public async Task DeleteFile_OwnedByAnotherUser_ReturnsNotFoundAndFileIsNotDeleted()
        {
            //Crear dos usuarios Admin
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.file.delete.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.file.delete.other", RolesList.Admin);

            //El dueño sube un archivo
            int fileId = await UploadSingleFileAsync(ownerClient, "owner-file-delete.png");

            //El otro usuario intenta eliminar el archivo del dueño
            HttpResponseMessage deleteResponse = await otherClient.DeleteAsync($"/api/files/{fileId}");

            //Debe devolver 404 con el código FileNotFound
            Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

            ApiObjResponse<object>? deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(deleteBody);
            Assert.False(deleteBody!.Result);
            Assert.Equal(ApiErrorCodes.FileNotFound, deleteBody.ErrorCode);

            //Verificar que el dueño real todavía puede descargar su archivo (no fue eliminado)
            HttpResponseMessage ownerDownload = await ownerClient.GetAsync($"/api/files/{fileId}");
            Assert.Equal(HttpStatusCode.OK, ownerDownload.StatusCode);
        }

        #endregion

        #region Ownership: Carpetas

        [Fact]
        public async Task GetFolder_OwnedByAnotherUser_ReturnsNotFoundWithFolderNotFoundCode()
        {
            //Crear dos usuarios Admin
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.folder.get.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.folder.get.other", RolesList.Admin);

            //El dueño crea una carpeta
            int folderId = await CreateFolderAsync(ownerClient, "OwnerFolder");

            //El otro usuario intenta obtener la carpeta del dueño
            HttpResponseMessage response = await otherClient.GetAsync($"/api/folders/{folderId}");

            //Debe devolver 404 con el código FolderNotFound
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.FolderNotFound, body.ErrorCode);
        }

        [Fact]
        public async Task UpdateFolder_OwnedByAnotherUser_ReturnsNotFoundWithFolderNotFoundCode()
        {
            //Crear dos usuarios Admin
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.folder.update.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.folder.update.other", RolesList.Admin);

            //El dueño crea una carpeta
            int folderId = await CreateFolderAsync(ownerClient, "OwnerFolderToRename");

            //El otro usuario intenta renombrar la carpeta del dueño
            UpdateFolderRequestDto updateDto = new UpdateFolderRequestDto
            {
                NewFolderName = "Hijacked"
            };
            HttpResponseMessage response = await otherClient.PatchAsJsonAsync($"/api/folders/{folderId}", updateDto);

            //Debe devolver 404 con el código FolderNotFound, sin modificar la carpeta ajena
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.FolderNotFound, body.ErrorCode);
        }

        [Fact]
        public async Task DeleteFolder_OwnedByAnotherUser_ReturnsNotFoundAndFolderIsNotDeleted()
        {
            //Crear dos usuarios Admin
            HttpClient ownerClient = await CreateAuthenticatedClientAsync("test.ownership.folder.delete.owner", RolesList.Admin);
            HttpClient otherClient = await CreateAuthenticatedClientAsync("test.ownership.folder.delete.other", RolesList.Admin);

            //El dueño crea una carpeta
            int folderId = await CreateFolderAsync(ownerClient, "OwnerFolderToDelete");

            //El otro usuario intenta eliminar la carpeta del dueño
            HttpResponseMessage deleteResponse = await otherClient.DeleteAsync($"/api/folders/{folderId}");

            //Debe devolver 404 con el código FolderNotFound
            Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

            ApiObjResponse<object>? deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(deleteBody);
            Assert.False(deleteBody!.Result);
            Assert.Equal(ApiErrorCodes.FolderNotFound, deleteBody.ErrorCode);

            //Verificar que el dueño real todavía puede obtener su carpeta (no fue eliminada)
            HttpResponseMessage ownerGet = await ownerClient.GetAsync($"/api/folders/{folderId}");
            Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
        }

        #endregion

        #region Permisos por módulo (RequireModule)

        [Fact]
        public async Task AccessFilesModule_WithoutFilesPermissionEnabled_ReturnsForbiddenWithUnauthorizedCode()
        {
            //Crear un admin (para poder registrar usuarios) y un usuario normal a través del endpoint de registro,
            //que crea los permisos de módulo por defecto (todos deshabilitados)
            HttpClient adminClient = await CreateAuthenticatedClientAsync("test.module.forbidden.admin", RolesList.Admin);
            (HttpClient regularClient, int _) = await RegisterAndLoginRegularUserAsync(adminClient, "test.module.forbidden.user");

            //El usuario normal, sin el módulo de archivos habilitado, intenta listar sus archivos
            HttpResponseMessage response = await regularClient.GetAsync("/api/files");

            //Debe devolver 403 con el código Unauthorized (mensaje genérico de módulo no habilitado)
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.Unauthorized, body.ErrorCode);
        }

        [Fact]
        public async Task AccessFilesModule_AfterAdminEnablesPermission_ReturnsOk()
        {
            //Crear un admin y un usuario normal (permisos de módulo deshabilitados por defecto)
            HttpClient adminClient = await CreateAuthenticatedClientAsync("test.module.enabled.admin", RolesList.Admin);
            (HttpClient regularClient, int regularUserId) = await RegisterAndLoginRegularUserAsync(adminClient, "test.module.enabled.user");

            //Confirmar que, antes de habilitar el módulo, el acceso está bloqueado
            HttpResponseMessage beforeResponse = await regularClient.GetAsync("/api/files");
            Assert.Equal(HttpStatusCode.Forbidden, beforeResponse.StatusCode);

            //El admin habilita el módulo de archivos para el usuario normal
            UpdateModulePermissionsRequestDto enableFilesDto = new UpdateModulePermissionsRequestDto
            {
                FilesEnabled = true
            };
            HttpResponseMessage patchResponse = await adminClient.PatchAsJsonAsync(
                $"/api/admin/users/{regularUserId}/permissions", enableFilesDto);
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            //Ahora el usuario normal debe poder acceder al módulo de archivos
            HttpResponseMessage afterResponse = await regularClient.GetAsync("/api/files");
            Assert.Equal(HttpStatusCode.OK, afterResponse.StatusCode);

            ApiObjResponse<IEnumerable<GetFileItemDto>>? body =
                await afterResponse.Content.ReadFromJsonAsync<ApiObjResponse<IEnumerable<GetFileItemDto>>>();
            Assert.NotNull(body);
            Assert.True(body!.Result);
        }

        [Fact]
        public async Task AccessFilesModule_AsAdmin_BypassesModulePermissionCheck()
        {
            //Crear un usuario Admin sin registro explícito de permisos de módulo
            HttpClient adminClient = await CreateAuthenticatedClientAsync("test.module.admin.bypass", RolesList.Admin);

            //El rol Admin es superusuario y no depende de UserModulePermissions para acceder a los módulos
            HttpResponseMessage response = await adminClient.GetAsync("/api/files");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion

        #region Helpers

        //Crea un usuario con el rol especificado, hace login en un HttpClient propio (independiente del Client
        //compartido de IntegrationTestBase) y lo devuelve ya autenticado.
        private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, RolesList role)
        {
            await Factory.CreateUserAsync(username, TestPassword, role);

            HttpClient client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
            LoginDto loginDto = new LoginDto(username, TestPassword);
            HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            return client;
        }

        //Usa un cliente Admin ya autenticado para registrar un nuevo usuario (rol User, permisos de módulo
        //deshabilitados por defecto) e inicia sesión con él en un HttpClient propio.
        private async Task<(HttpClient Client, int UserId)> RegisterAndLoginRegularUserAsync(HttpClient adminClient, string username)
        {
            RegisterDto registerDto = new RegisterDto(username, TestPassword);
            HttpResponseMessage registerResponse = await adminClient.PostAsJsonAsync("/api/auth/register", registerDto);
            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            ApiObjResponse<UserDto>? registerBody =
                await registerResponse.Content.ReadFromJsonAsync<ApiObjResponse<UserDto>>();
            Assert.NotNull(registerBody);
            Assert.NotNull(registerBody!.Data);

            HttpClient regularClient = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
            LoginDto loginDto = new LoginDto(username, TestPassword);
            HttpResponseMessage loginResponse = await regularClient.PostAsJsonAsync("/api/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            return (regularClient, registerBody.Data!.Id);
        }

        //Sube un único archivo (chunk único con firma PNG) usando el cliente indicado y devuelve el Id resultante.
        private static async Task<int> UploadSingleFileAsync(HttpClient client, string fileName)
        {
            byte[] chunk = new byte[ChunkSizeBytes];
            new Random(fileName.GetHashCode()).NextBytes(chunk);
            Array.Copy(PngSignature, chunk, PngSignature.Length);

            UploadInitRequestDto initRequest = new UploadInitRequestDto
            {
                FileName = fileName,
                TotalSizeBytes = chunk.Length,
                TotalChunks = 1,
                FolderId = null
            };

            HttpResponseMessage initResponse = await client.PostAsJsonAsync("/api/files/upload/init", initRequest);
            ApiObjResponse<UploadInitResponseDto>? initBody =
                await initResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadInitResponseDto>>();
            Guid sessionId = initBody!.Data!.SessionId;

            using MultipartFormDataContent chunkContent = new MultipartFormDataContent
            {
                { new StringContent(sessionId.ToString()), "sessionId" },
                { new StringContent("1"), "chunkNumber" }
            };
            ByteArrayContent chunkFileContent = new ByteArrayContent(chunk);
            chunkFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            chunkContent.Add(chunkFileContent, "chunk", "chunk.bin");

            HttpResponseMessage chunkResponse = await client.PostAsync("/api/files/upload/chunk", chunkContent);
            Assert.Equal(HttpStatusCode.OK, chunkResponse.StatusCode);

            HttpResponseMessage completeResponse = await client.PostAsync($"/api/files/upload/{sessionId}/complete", null);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

            ApiObjResponse<UploadFileResponseDto>? completeBody =
                await completeResponse.Content.ReadFromJsonAsync<ApiObjResponse<UploadFileResponseDto>>();

            return completeBody!.Data!.Id;
        }

        //Crea una carpeta usando el cliente indicado y devuelve el Id resultante.
        private static async Task<int> CreateFolderAsync(HttpClient client, string folderName)
        {
            CreateFolderRequestDto createDto = new CreateFolderRequestDto(folderName, null);
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/folders", createDto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            ApiObjResponse<CreateFolderResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<CreateFolderResponseDto>>();

            return body!.Data!.Id;
        }

        #endregion
    }
}