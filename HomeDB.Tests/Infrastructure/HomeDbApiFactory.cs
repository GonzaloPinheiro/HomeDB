using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;
using HomeDB.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

        //Configura el host web para usar la cadena de conexión del contenedor de PostgreSQL
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //Configura la cadena de conexión a la base de datos para que apunte al contenedor de PostgreSQL
            builder.UseSetting("ConnectionStrings:PostgreSQL_HomeDB", _postgresContainer.GetConnectionString());
            builder.UseSetting("RateLimiting:Auth:Enabled", "false");
            builder.UseSetting("RateLimiting:Global:Enabled", "false");
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
                }
            };

            //Se agrega el usuario a la base de datos y se guarda
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return user.Id;
        }
    }
}
