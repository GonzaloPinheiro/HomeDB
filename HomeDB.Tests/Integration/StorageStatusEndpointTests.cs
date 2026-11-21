using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Auth;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    //Tests del endpoint GET api/storage/locations: forma de la respuesta, disco caído y permisos del módulo SystemMonitor.
    public sealed class StorageStatusEndpointTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string LocationsEndpoint = "/api/storage/locations";
        private const string TestPassword = "Password123!";

        //Constructores
        public StorageStatusEndpointTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetLocations_AsAdmin_ReturnsEveryConfiguredLocationWithCapacity()
        {
            await CreateUserAndLoginAsync("test.storage.status.admin", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await Client.GetAsync(LocationsEndpoint);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<StorageLocationStatusDto> locations = await ReadLocationsAsync(response);

            //El disco de pruebas (con marcador) tiene dos ubicaciones: main (Personal) y temp (Temp)
            List<StorageLocationStatusDto> testDisk = locations.Where(l => l.DiskKey == "test").ToList();
            Assert.Equal(new[] { "main", "temp" }, testDisk.Select(l => l.LocationKey).ToArray());
            Assert.All(testDisk, location =>
            {
                Assert.True(location.IsAvailable);
                Assert.True(location.IsWritable);
                Assert.Null(location.Reason);
                Assert.True(location.TotalBytes > 0);
                Assert.Equal(location.TotalBytes - location.FreeBytes, location.UsedBytes);
            });
            Assert.Equal(LocationUsage.Personal, testDisk[0].Usage);
            Assert.Equal(LocationUsage.Temp, testDisk[1].Usage);

            //La API de pruebas hereda también los discos de appsettings.json (/das/hdd1 y /das/hdd2), que no existen
            //aquí: deben salir como no disponibles y sin capacidad, no con la del disco que contiene esa ruta.
            List<StorageLocationStatusDto> missingDisks = locations.Where(l => l.DiskKey != "test").ToList();
            Assert.NotEmpty(missingDisks);
            Assert.All(missingDisks, location =>
            {
                Assert.False(location.IsAvailable);
                Assert.Null(location.TotalBytes);
            });
        }

        [Fact]
        public async Task GetLocations_WhenDiskUnavailable_Returns200WithUnavailableLocationsAndNoCapacity()
        {
            await CreateUserAndLoginAsync("test.storage.status.down", TestPassword, RolesList.Admin);
            IStorageAvailabilityService availability = Factory.Services.GetRequiredService<IStorageAvailabilityService>();

            try
            {
                using (Factory.SimulateDiskUnavailable())
                {
                    //Equivale a un ciclo del vigilante, que es quien actualiza el estado guardado
                    availability.CheckAllDisks();

                    HttpResponseMessage response = await Client.GetAsync(LocationsEndpoint);

                    //Es una consulta de estado: un disco caído no es un error de la petición
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    List<StorageLocationStatusDto> locations = await ReadLocationsAsync(response);

                    List<StorageLocationStatusDto> testDisk = locations.Where(l => l.DiskKey == "test").ToList();
                    Assert.Equal(2, testDisk.Count);
                    Assert.All(testDisk, location =>
                    {
                        Assert.False(location.IsAvailable);
                        Assert.Equal("marker file is missing", location.Reason);
                        Assert.Null(location.TotalBytes);
                        Assert.Null(location.UsedBytes);
                        Assert.Null(location.FreeBytes);
                    });
                }
            }
            finally
            {
                //El marcador ya está restaurado: refrescar el estado guardado para no afectar a otros tests
                availability.CheckAllDisks();
            }
        }

        [Fact]
        public async Task GetLocations_WithoutLogin_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await Client.GetAsync(LocationsEndpoint);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLocations_RegularUserWithoutSystemMonitorModule_ReturnsForbidden()
        {
            await CreateUserAndLoginAsync("test.storage.status.regular.admin", TestPassword, RolesList.Admin);

            //El registro por el endpoint crea los permisos de módulo por defecto (todos deshabilitados)
            RegisterDto registerDto = new RegisterDto("test.storage.status.regular", TestPassword);
            HttpResponseMessage registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            HttpClient regularClient = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
            HttpResponseMessage loginResponse = await regularClient.PostAsJsonAsync(
                "/api/auth/login", new LoginDto("test.storage.status.regular", TestPassword));
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            HttpResponseMessage response = await regularClient.GetAsync(LocationsEndpoint);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.Equal(ApiErrorCodes.Unauthorized, body!.ErrorCode);
        }

        //Helpers
        private static async Task<List<StorageLocationStatusDto>> ReadLocationsAsync(HttpResponseMessage response)
        {
            ApiObjResponse<List<StorageLocationStatusDto>>? body =
                await response.Content.ReadFromJsonAsync<ApiObjResponse<List<StorageLocationStatusDto>>>();

            Assert.NotNull(body);
            Assert.True(body!.Result);
            Assert.NotNull(body.Data);

            return body.Data!;
        }
    }
}
