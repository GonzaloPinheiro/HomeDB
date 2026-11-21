using HomeDB.Application.DTOs;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace HomeDB.Tests.Integration
{
    //Tests de integración del backup manual con el disco de backups no disponible.
    //La API de pruebas hereda de appsettings.json el disco "hdd2" (ubicación de backups, en /das/hdd2), que no existe
    //en el entorno de tests: por eso el disco de backups ya está ausente sin necesidad de simularlo.
    public sealed class BackupEndpointTests : IntegrationTestBase
    {
        //Variables y objetos
        private const string TriggerEndpoint = "/api/backup/Daily/trigger";
        private const string HistoryEndpoint = "/api/backup/history";
        private const string TestPassword = "Password123!";

        //Constructores
        public BackupEndpointTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task TriggerBackup_WhenBackupsDiskUnavailable_Returns503_AndRegistersNothing()
        {
            await CreateUserAndLoginAsync("test.backup.disk.down", TestPassword, RolesList.Admin);

            HttpResponseMessage response = await Client.PostAsync(TriggerEndpoint, null);

            //El backup se aborta antes de rotar nada o crear el registro de auditoría
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            ApiObjResponse<object>? body = await response.Content.ReadFromJsonAsync<ApiObjResponse<object>>();
            Assert.NotNull(body);
            Assert.False(body!.Result);
            Assert.Equal(ApiErrorCodes.StorageLocationUnavailable, body.ErrorCode);
            Assert.Contains("hdd2", body.ErrorMessage);

            //No queda ningún registro en el historial: ni siquiera uno fallido
            HttpResponseMessage historyResponse = await Client.GetAsync(HistoryEndpoint);
            Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
            ApiObjResponse<GetBackupHistoryResponseDto>? history =
                await historyResponse.Content.ReadFromJsonAsync<ApiObjResponse<GetBackupHistoryResponseDto>>();
            Assert.NotNull(history?.Data);
            Assert.Equal(0, history!.Data!.TotalCount);
        }
    }
}
