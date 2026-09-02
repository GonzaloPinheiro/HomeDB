using HomeDB.Common;
using HomeDB.Domain.Common;
using HomeDB.Tests.Infrastructure;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration.AuthTests
{
    public sealed class LogoutTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string LogoutEndpoint = "/api/auth/logout";
        private const string RefreshEndpoint = "/api/auth/refreshToken";
        private const string TestPassword = "Password123!";

        //Constructores
        public LogoutTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Logout_WhenAuthenticated_ReturnsSuccessAndDeletesCookies()
        {
            //Crear usuario y hacer login para obtener cookies de autenticación
            const string username = "test.logout.valid";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Hacer el logout
            HttpResponseMessage response = await Client.PostAsync(LogoutEndpoint, null);

            //Comprobar que la respuesta es correcta
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //Comprobar que existe el body y que el resultado es true
            ApiObjResponse<object>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.True(body!.Result);

            //Comprobar que existen las cookies de AccessToken y RefreshToken en el header del hhtp
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out System.Collections.Generic.IEnumerable<string>? rawCookies));

            System.Collections.Generic.List<SetCookieHeaderValue> cookies =
                SetCookieHeaderValue.ParseList(new System.Collections.Generic.List<string>(rawCookies!)).ToList();

            SetCookieHeaderValue? accessCookie = cookies.Find(c => c.Name == nameof(CookieNames.AccessToken));
            SetCookieHeaderValue? refreshCookie = cookies.Find(c => c.Name == nameof(CookieNames.RefreshToken));

            Assert.NotNull(accessCookie);
            Assert.NotNull(refreshCookie);

            //Comprobar que las cookies tienen fecha de expiración en el pasado (lo que indica que han sido eliminadas)
            Assert.True(accessCookie!.Expires < DateTimeOffset.UtcNow);
            Assert.True(refreshCookie!.Expires < DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
        {
            //Hacer el logout sin estar autenticado
            HttpResponseMessage response = await Client.PostAsync(LogoutEndpoint, null);

            //Comprobar que la respuesta es 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_InvalidatesRefreshToken_SoSubsequentRefreshFails()
        {
            //Crear usuario y hacer login para obtener cookies de autenticación
            const string username = "test.logout.valid";
            await CreateUserAndLoginAsync(username, TestPassword, RolesList.Admin);

            //Hacer el logout
            HttpResponseMessage logoutResponse = await Client.PostAsync(LogoutEndpoint, null);

            //Comprobar que la respuesta es correcta
            Assert.Equal(HttpStatusCode.Continue, logoutResponse.StatusCode);

            //Intentar hacer refresh con la cookie de refresh ya usada
            HttpResponseMessage refreshResponse = await Client.PostAsync(RefreshEndpoint, null);

            //Comprobar que la respuesta es 401 Unauthorized, indicando que el refresh token ya no es válido
            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        }
    }
}