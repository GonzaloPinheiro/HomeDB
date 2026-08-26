using HomeDB.Application.DTOs.Auth;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    public sealed class LoginTests : IntegrationTestBase
    {
        private const string LoginEndpoint = "/api/auth/login";
        private const string TestPassword = "Password123!";

        //Nada que inicializar, todo se hereda de IntegrationTestBase
        public LoginTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsSuccessWithTokens()
        {
            //Crea un usuario en la DB
            const string username = "test.login.user";
            await Factory.CreateUserAsync(username, TestPassword, RolesList.Admin);

            //Crear el objeto de login con las credenciales correctas
            LoginDto loginDto = new LoginDto(username, TestPassword);

            //Hacer la petición POST al endpoint de login y capturar la respuesta
            HttpResponseMessage response = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Comprobar que la respuesta es 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //Leer el cuerpo de la respuesta como ApiObjResponse<TokenResponseDto>
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Comprobar que el cuerpo no es nulo, que el resultado es true y que los datos no son nulos
            Assert.NotNull(body);
            Assert.True(body!.Result);
            Assert.NotNull(body.Data);

            //Comprobar que el http trae el header Set-Cookie
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out System.Collections.Generic.IEnumerable<string>? rawCookies));

            //Parsear las cookies a una variable
            System.Collections.Generic.List<SetCookieHeaderValue> cookies =
                SetCookieHeaderValue.ParseList(new System.Collections.Generic.List<string>(rawCookies!)).ToList();

            //Buscar las cookies de access y refresh token por su nombre
            SetCookieHeaderValue? accessCookie = cookies.Find(c => c.Name == nameof(CookieNames.AccessToken));
            SetCookieHeaderValue? refreshCookie = cookies.Find(c => c.Name == nameof(CookieNames.RefreshToken));

            //Comprobar que las cookies no son nulas y que son HttpOnly
            Assert.NotNull(accessCookie);
            Assert.True(accessCookie!.HttpOnly);

            Assert.NotNull(refreshCookie);
            Assert.True(refreshCookie!.HttpOnly);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorizedWithInvalidCredentialsCode()
        {
            //Crea un usuario en la DB
            const string username = "test.login.invalid";
            await Factory.CreateUserAsync(username, TestPassword, RolesList.Admin);

            //Crea el objeto de login con la contraseña incorrecta
            LoginDto loginDto = new LoginDto(username, "WrongPassword!");

            //Hacer la petición POST al endpoint de login y capturar la respuesta
            HttpResponseMessage response = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Verificar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            //Parsear el cuerpo de la respuesta como ApiObjResponse<TokenResponseDto>
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Verificar que el cuerpo no es nulo, que el resultado es false, que los datos son nulos y que el código de error es InvalidCredentials
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Null(body.Data);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsUnauthorizedWithInvalidCredentialsCode()
        {
            //Crea el objeto de login con un usuario que no existe
            LoginDto loginDto = new LoginDto("user.that.does.not.exist", TestPassword);

            //Hacer la petición POST al endpoint de login y capturar la respuesta
            HttpResponseMessage response = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Verificar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            //Parsear el cuerpo de la respuesta como ApiObjResponse<TokenResponseDto>
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Verificar que el cuerpo no es nulo, que el resultado es false, que los datos son nulos y que el código de error es InvalidCredentials
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }

        [Fact]
        public async Task Login_WithEmptyCredentials_ReturnsUnauthorizedWithInvalidCredentialsCode()
        {
            //Crea el objeto de login con credenciales vacías
            LoginDto loginDto = new LoginDto(string.Empty, string.Empty);

            //Hacer la petición POST al endpoint de login y capturar la respuesta
            HttpResponseMessage response = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Verificar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            //Parsear el cuerpo de la respuesta como ApiObjResponse<TokenResponseDto>
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Verificar que el cuerpo no es nulo, que el resultado es false, que los datos son nulos y que el código de error es InvalidCredentials
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }

        // Activar y borrar el test de arriba cuando LoginDto pase a class con [Required]
        // en Username y Password. Con [Required], .NET rechaza la petición antes de llegar al endpoint con un 400
        //
        // [Fact]
        // public async Task Login_WithEmptyCredentials_ReturnsBadRequestFromModelValidation()
        // {
        //     //Crea el objeto de login con credenciales vacías
        //     LoginDto loginDto = new LoginDto(string.Empty, string.Empty);
        //
        //     //Hacer la petición POST al endpoint de login y capturar la respuesta
        //     HttpResponseMessage response = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);
        //
        //     //Comprobar que la respuesta es 400 Bad Request
        //     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // }
    }
}