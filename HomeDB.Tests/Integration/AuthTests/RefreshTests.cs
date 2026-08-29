using HomeDB.Application.DTOs.Auth;
using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration.AuthTests
{
    public sealed class RefreshTests : IntegrationTestBase
    {
        private const string RefreshEndpoint = "/api/auth/refreshToken";
        private const string TestPassword = "Password123!";

        public RefreshTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Refresh_WithValidRefreshTokenCookie_ReturnsNewTokens()
        {
            //Crea un usuario y hace login
            const string username = "test.refresh.valid";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Hacer el post al endpoint de refresh token con la cookie de refresh token
            HttpResponseMessage response = await Client.PostAsync(RefreshEndpoint, null);

            //Comprobar que la respuesta es 200 Ok
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //Comprobar que la respuesta contiene un body
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Comprobar que el body no es nulo y contiene los nuevos tokens
            Assert.NotNull(body);
            Assert.True(body!.Result);
            Assert.NotNull(body.Data);

            //Comprobar que el http contiene el header Set-Cookie con los nuevos tokens
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out System.Collections.Generic.IEnumerable<string>? rawCookies));

            //Parsear los cookies del header Set-Cookie
            System.Collections.Generic.List<SetCookieHeaderValue> cookies =
                SetCookieHeaderValue.ParseList(new System.Collections.Generic.List<string>(rawCookies!)).ToList();

            //Comprobar que los cookies contienen el access token y el refresh token
            SetCookieHeaderValue? accessCookie = cookies.Find(c => c.Name == nameof(CookieNames.AccessToken));
            SetCookieHeaderValue? refreshCookie = cookies.Find(c => c.Name == nameof(CookieNames.RefreshToken));

            //Comprobar que los cookies no son nulos
            Assert.NotNull(accessCookie);
            Assert.NotNull(refreshCookie);
        }

        [Fact]
        public async Task Refresh_WithoutRefreshTokenCookie_ReturnsUnauthorized()
        {
            //Hacer el post sin cookie de refresh token
            HttpResponseMessage response = await Client.PostAsync(RefreshEndpoint, null);

            //Comprobar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            //Comprobar que la respuesta contiene un body
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Comprobar que el body no es nulo y contiene el error de credenciales inválidas
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }

        [Fact]
        public async Task Refresh_WithInvalidRefreshTokenValue_ReturnsUnauthorized()
        {
            //Crear un HttpRequestMessage con un valor de refresh token inválido en la cookie
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RefreshEndpoint);
            request.Headers.Add("Cookie", $"{nameof(CookieNames.RefreshToken)}=this-is-not-a-valid-token");

            //Hacer el post al endpoint de refresh token con la cookie inválida
            HttpResponseMessage response = await Client.SendAsync(request);

            //Comprobar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            //Comprobar que la respuesta contiene un body
            ApiObjResponse<TokenResponseDto>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Comprobar que el body no es nulo y contiene el error de credenciales inválidas
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }

        [Fact]
        public async Task Refresh_WithAlreadyUsedRefreshToken_ReturnsUnauthorized()
        {
            //Crea un usuario y hace login
            const string username = "test.refresh.reuse";
            HttpResponseMessage loginResponse = await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Comprobar que el http contiene el header Set-Cookie con los tokens del login
            Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out System.Collections.Generic.IEnumerable<string>? rawLoginCookies));

            //Parsear los cookies del header Set-Cookie del login
            System.Collections.Generic.List<SetCookieHeaderValue> loginCookies =
                SetCookieHeaderValue.ParseList(new System.Collections.Generic.List<string>(rawLoginCookies!)).ToList();

            //Obtener el valor del refresh token
            SetCookieHeaderValue? originalRefreshCookie = loginCookies.Find(c => c.Name == nameof(CookieNames.RefreshToken));
            Assert.NotNull(originalRefreshCookie);
            string originalRefreshToken = originalRefreshCookie!.Value.ToString();

            //Hacer el primer post al endpoint de refresh token con la cookie de refresh token del login
            HttpResponseMessage firstRefreshResponse = await Client.PostAsync(RefreshEndpoint, null);
            Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

            //Crear un nuevo HttpClient y un nuevo HttpRequestMessage con el refresh token original del login(revocado)
            using HttpClient reuseClient = Factory.CreateClient();
            using HttpRequestMessage reuseRequest = new HttpRequestMessage(HttpMethod.Post, RefreshEndpoint);
            reuseRequest.Headers.Add("Cookie", $"{nameof(CookieNames.RefreshToken)}={originalRefreshToken}");

            //Hacer el post al endpoint de refresh token con la cookie del refresh token original (revocado)
            HttpResponseMessage reuseResponse = await reuseClient.SendAsync(reuseRequest);

            //Comprobar que la respuesta es 401
            Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

            //Comprobar que la respuesta contiene un body con el error de credenciales inválidas
            ApiObjResponse<TokenResponseDto>? body =
                await reuseResponse.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            //Comprobar que el body no es nulo y contiene el error de credenciales inválidas
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.InvalidCredentials, body.ErrorCode);
        }
    }
}
