using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Data;
using HomeDB.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeDB.Tests.Integration
{
    //Tests de la política de arranque sobre la API real: se levanta una segunda instancia (a partir de la
    //configuración de la factory compartida) cuyo disco principal apunta a una carpeta sin fichero marcador.
    public sealed class StorageStartupPolicyTests : IntegrationTestBase
    {
        //Constructores
        public StorageStartupPolicyTests(HomeDbApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public void Startup_RequiredDiskWithoutMarker_ApiDoesNotStart()
        {
            string diskWithoutMarker = Directory.CreateTempSubdirectory("HomeDB.Tests.NoMarker").FullName;

            try
            {
                using WebApplicationFactory<Program> factory = Factory.WithWebHostBuilder(builder =>
                    builder.UseSetting("Storage:Disks:0:Path", diskWithoutMarker));

                //El disco 0 es Required: el arranque debe abortar con un error que explique el motivo
                Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

                Assert.Contains("StartupPolicy=Required", FlattenMessages(exception));
                Assert.Contains("marker file is missing", FlattenMessages(exception));
            }
            finally
            {
                Directory.Delete(diskWithoutMarker, recursive: true);
            }
        }

        [Fact]
        public async Task Startup_OptionalDiskWithoutMarker_ApiStarts_AndLogsCritical()
        {
            string diskWithoutMarker = Directory.CreateTempSubdirectory("HomeDB.Tests.NoMarker").FullName;

            try
            {
                //Base de datos propia: la segunda API migra desde cero y no choca con la de los demás tests
                string isolatedConnectionString = await Factory.CreateIsolatedDatabaseAsync();

                await using WebApplicationFactory<Program> factory = Factory.WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("ConnectionStrings:PostgreSQL_HomeDB", isolatedConnectionString);
                    builder.UseSetting("Storage:Disks:0:Path", diskWithoutMarker);
                    builder.UseSetting("Storage:Disks:0:StartupPolicy", "Optional");
                });

                //La API arranca aunque falte el disco
                using HttpClient client = factory.CreateClient();
                HttpResponseMessage health = await client.GetAsync("/health");
                Assert.True(health.StatusCode is not System.Net.HttpStatusCode.NotFound);

                //El aviso Critical se persiste cuando arranca el worker de logs: se espera con polling
                LogEntry? critical = await WaitForLogAsync(factory.Services, entry =>
                    entry.Level == "Critical" && entry.Message.Contains("no está disponible al arrancar"));

                Assert.NotNull(critical);
                Assert.Contains("'test'", critical!.Message);
                Assert.Contains("marker file is missing", critical.Message);
            }
            finally
            {
                Directory.Delete(diskWithoutMarker, recursive: true);
            }
        }

        #region Helpers
        //Concatena el mensaje de la excepción y los de todas sus excepciones internas
        private static string FlattenMessages(Exception exception)
        {
            List<string> messages = new List<string>();
            for (Exception? current = exception; current != null; current = current.InnerException)
                messages.Add(current.Message);

            return string.Join(" | ", messages);
        }

        //Busca en la base de datos de la API indicada un log que cumpla el filtro, esperando hasta 10 s a que el worker lo inserte
        private static async Task<LogEntry?> WaitForLogAsync(IServiceProvider services, Func<LogEntry, bool> filter)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);

            while (DateTime.UtcNow < deadline)
            {
                using IServiceScope scope = services.CreateScope();
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                List<LogEntry> logs = await dbContext.Logs.AsNoTracking().Where(l => l.Level == "Critical").ToListAsync();
                LogEntry? match = logs.FirstOrDefault(filter);
                if (match != null)
                    return match;

                await Task.Delay(200);
            }

            return null;
        }
        #endregion
    }
}
