using System.Net;
using System.Net.Http.Json;
using HomeDB.Application.DTOs.Auth;
using HomeDB.Domain.Common;

namespace HomeDB.Tests.Infrastructure
{
    //Base usada para que cada test tenga su propia instancia de la API y no se afecten entre sí teniendo datos limpios
    [Collection("HomeDbApi")]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        //Variables y objetos
        protected readonly HomeDbApiFactory Factory;
        protected readonly HttpClient Client;

        //Constructores
        protected IntegrationTestBase(HomeDbApiFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
        }

        //Implementación de IAsyncLifetime para inicializar y limpiar la base de datos antes y después de cada prueba
        public async Task InitializeAsync()
        {
            await Factory.ResetDatabaseAsync();
        }

        //Implementación de IAsyncLifetime para limpiar recursos al finalizar las pruebas
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        #region Helpers
        //Crea un usuario y hace login, 
        protected async Task<HttpResponseMessage> CreateUserAndLoginAsync(string username, string password, RolesList role)
        {
            //Endpoint de login
            const string LoginEndpoint = "/api/auth/login";

            //Crea un usuario con el rol especificado
            await Factory.CreateUserAsync(username, password, role);

            //Hace login con el usuario creado
            LoginDto loginDto = new LoginDto(username, password);
            HttpResponseMessage loginResponse = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Verifica que el login fue exitoso
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            return loginResponse;

            ////Leer el cuerpo de la respuesta como ApiObjResponse<TokenResponseDto>
            //ApiObjResponse<TokenResponseDto>? body =
            //    await loginResponse.Content.ReadFromJsonAsync<ApiObjResponse<TokenResponseDto>>();

            ////Comprobar que el cuerpo no es nulo, que el resultado es true y que los datos no son nulos
            //Assert.NotNull(body);
            //Assert.True(body!.Result);
            //Assert.NotNull(body.Data);
        }

        //Hace login
        protected async Task<HttpResponseMessage> LoginAsync(string username, string password, RolesList role)
        {
            //Endpoint de login
            const string LoginEndpoint = "/api/auth/login";

            //Hace login con el usuario creado
            LoginDto loginDto = new LoginDto(username, password);
            HttpResponseMessage loginResponse = await Client.PostAsJsonAsync(LoginEndpoint, loginDto);

            //Verifica que el login fue exitoso
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            return loginResponse;
        }
        #endregion
    }
}
