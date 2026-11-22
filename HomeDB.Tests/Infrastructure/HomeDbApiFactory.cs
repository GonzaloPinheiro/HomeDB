using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Data;
using HomeDB.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;


namespace HomeDB.Tests.Infrastructure
{
    //Factory para crear un host web de prueba con un contenedor de PostgreSQL
    public sealed class HomeDbApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        //Configuración del contenedor de PostgreSQL para pruebas
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("homedb_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        //Conexión a la base de datos y Respawner para resetear el estado de la base de datos entre pruebas
        private NpgsqlConnection _dbConnection = null!;
        private Respawner _respawner = null!;

        //Se crea un directorio temporal único para cada ejecución de pruebas usando un GUID para evitar colisiones.
        private readonly string _storageRootPath = Path.Combine(Path.GetTempPath(), "HomeDB.Tests", Guid.NewGuid().ToString());

        private const string TestDiskKey = "test";

        //Configura el host web para usar la cadena de conexión del contenedor de PostgreSQL
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //El disco de pruebas debe existir y llevar su fichero marcador (con la clave del disco como contenido),
            //igual que un disco real: sin él, la API lo trata como no disponible y responde 503.
            Directory.CreateDirectory(_storageRootPath);
            File.WriteAllText(Path.Combine(_storageRootPath, StorageAvailabilityService.MarkerFileName), TestDiskKey);

            //Configura la cadena de conexión a la base de datos para que apunte al contenedor de PostgreSQL
            builder.UseSetting("ConnectionStrings:PostgreSQL_HomeDB", _postgresContainer.GetConnectionString());
            builder.UseSetting("RateLimiting:Auth:Enabled", "false");
            builder.UseSetting("RateLimiting:Global:Enabled", "false");
            builder.UseSetting("Jwt:Key", "TestOnlyJwtSigningKey_MustBeAtLeast32CharsLong");
            builder.UseSetting("Jwt:Issuer", "HomeDB_Test_Issuer");
            builder.UseSetting("Storage:Disks:0:Key", TestDiskKey);
            builder.UseSetting("Storage:Disks:0:Path", _storageRootPath);
            builder.UseSetting("Storage:Disks:0:StartupPolicy", "Required");
            builder.UseSetting("Storage:Disks:0:MinFreeBytes", "0");
            builder.UseSetting("Storage:Disks:0:Locations:0:Key", "main");
            builder.UseSetting("Storage:Disks:0:Locations:0:Usage", "Personal");
            builder.UseSetting("Storage:Disks:0:Locations:0:IsDefault", "true");
            builder.UseSetting("Storage:Disks:0:Locations:0:SubPath", "files");
            builder.UseSetting("Storage:Disks:0:Locations:0:IncludeInBackup", "true");
            builder.UseSetting("Storage:Disks:0:Locations:1:Key", "temp");
            builder.UseSetting("Storage:Disks:0:Locations:1:Usage", "Temp");
            builder.UseSetting("Storage:Disks:0:Locations:1:IsDefault", "true");
            builder.UseSetting("Storage:Disks:0:Locations:1:SubPath", "temp-uploads");
            builder.UseSetting("Storage:Disks:0:Locations:1:IncludeInBackup", "false");
            builder.UseSetting("LogFallback:BasePath", Path.Combine(_storageRootPath, "logs", "failed-logs"));
        }

        //Implementación de IAsyncLifetime para inicializar el contenedor y Respawner antes de las pruebas
        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            //Forzar la creación del host y ejecución de migraciones antes de capturar el estado inicial con Respawn.
            using IServiceScope scope = Services.CreateScope();

            //Obtiene el contexto de la base de datos y fuerza la creación de la base de datos y la ejecución de migraciones
            _dbConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
            await _dbConnection.OpenAsync();

            //Configura Respawn para ignorar la inserción de los roles en la tabla "roles" de nuevo.
            _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = new Respawn.Graph.Table[] { "roles" }
            });
        }

        //Simula que el disco de pruebas deja de estar disponible borrando su fichero marcador, que es lo que
        //ocurre en la práctica cuando un disco no está montado. Al hacer Dispose del valor devuelto el marcador
        //se restaura, así que el uso correcto es con "using" para no afectar a otros tests de la misma colección.
        public IDisposable SimulateDiskUnavailable()
        {
            string markerPath = Path.Combine(_storageRootPath, StorageAvailabilityService.MarkerFileName);
            File.Delete(markerPath);
            return new DiskMarkerRestorer(markerPath, TestDiskKey);
        }

        //Cambia en caliente los límites de espacio del disco de pruebas (MinFreeBytes) y de su ubicación "main" (MaxUsedBytes),
        //modificando las opciones que comparte la API. Al hacer Dispose del valor devuelto se restauran los valores
        //anteriores, así que el uso correcto es con "using" para no afectar a otros tests de la misma colección.
        public IDisposable LimitStorage(long minFreeBytes, long? maxUsedBytes)
        {
            StorageOptions storageOptions = Services.GetRequiredService<IOptions<StorageOptions>>().Value;
            DiskOptions disk = storageOptions.Disks.Single(d => d.Key == TestDiskKey);
            StorageLocationOptions main = disk.Locations.Single(l => l.Key == "main");

            long previousMinFreeBytes = disk.MinFreeBytes;
            long? previousMaxUsedBytes = main.MaxUsedBytes;

            disk.MinFreeBytes = minFreeBytes;
            main.MaxUsedBytes = maxUsedBytes;

            return new StorageLimitsRestorer(() =>
            {
                disk.MinFreeBytes = previousMinFreeBytes;
                main.MaxUsedBytes = previousMaxUsedBytes;
            });
        }

        //Cambia en caliente el máximo de la subida directa (Storage:SingleUploadMaxFileBytes). Al hacer Dispose del
        //valor devuelto se restaura el anterior, así que el uso correcto es con "using".
        public IDisposable LimitSingleUpload(long maxFileBytes)
        {
            StorageOptions storageOptions = Services.GetRequiredService<IOptions<StorageOptions>>().Value;
            long previousMaxFileBytes = storageOptions.SingleUploadMaxFileBytes;

            storageOptions.SingleUploadMaxFileBytes = maxFileBytes;

            return new StorageLimitsRestorer(() => storageOptions.SingleUploadMaxFileBytes = previousMaxFileBytes);
        }

        private sealed class StorageLimitsRestorer : IDisposable
        {
            private readonly Action _restore;

            public StorageLimitsRestorer(Action restore)
            {
                _restore = restore;
            }

            public void Dispose()
            {
                _restore();
            }
        }

        private sealed class DiskMarkerRestorer : IDisposable
        {
            private readonly string _markerPath;
            private readonly string _diskKey;

            public DiskMarkerRestorer(string markerPath, string diskKey)
            {
                _markerPath = markerPath;
                _diskKey = diskKey;
            }

            public void Dispose()
            {
                File.WriteAllText(_markerPath, _diskKey);
            }
        }

        //Crea una base de datos vacía y nueva en el mismo contenedor de PostgreSQL y devuelve su cadena de conexión.
        //Sirve para levantar una segunda instancia de la API sin chocar con la compartida: Respawn vacía también la
        //tabla de historial de migraciones de la base de datos compartida, así que una segunda API que migrase
        //sobre ella intentaría recrear tablas que ya existen. El contenedor entero se elimina al terminar.
        public async Task<string> CreateIsolatedDatabaseAsync()
        {
            string databaseName = "homedb_test_" + Guid.NewGuid().ToString("N");

            await using NpgsqlCommand command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", _dbConnection);
            await command.ExecuteNonQueryAsync();

            NpgsqlConnectionStringBuilder connectionStringBuilder =
                new NpgsqlConnectionStringBuilder(_postgresContainer.GetConnectionString()) { Database = databaseName };

            return connectionStringBuilder.ConnectionString;
        }

        //Método para resetear la base de datos a su estado inicial usando Respawn
        public async Task ResetDatabaseAsync()
        {
            await _respawner.ResetAsync(_dbConnection);
        }

        //Implementación de IAsyncLifetime para limpiar recursos al finalizar las pruebas
        public new async Task DisposeAsync()
        {
            await _dbConnection.DisposeAsync();
            await _postgresContainer.DisposeAsync();
            await base.DisposeAsync();

            //Eliminar el directorio temporal usado para el almacenamiento de ficheros de test
            if (Directory.Exists(_storageRootPath))
            {
                Directory.Delete(_storageRootPath, recursive: true);
            }
        }

        //Método de utilidad para crear un usuario en la base de datos para pruebas
        public async Task<int> CreateUserAsync(string username, string password, RolesList role)
        {
            //Scope propio para no reutilizar el DbContext de otras operaciones
            using IServiceScope scope = Services.CreateScope();

            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IPasswordHelper passwordHelper = scope.ServiceProvider.GetRequiredService<IPasswordHelper>();

            //Se hashea la contraseña igual que hace AuthService al registrar un usuario
            User user = new User
            {
                Username = username,
                PasswordHash = passwordHelper.HashPassword(password),
                //Asignación del rol mediante UserRole, como en el flujo real de registro
                UserRoles = new List<UserRole>
                {
                    new UserRole { RoleId = (int)role }
                },
                //Agregar configuración de usuario por defecto
                AdminSettings = new UserAdminSettings()
            };

            //Se agrega el usuario a la base de datos y se guarda
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return user.Id;
        }
    }
}
