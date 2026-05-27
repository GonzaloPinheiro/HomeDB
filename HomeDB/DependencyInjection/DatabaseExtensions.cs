using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.DependencyInjection
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Registra el AppDbContext en dos modalidades: como DbContextFactory (para singletons
        /// como el logger) y como DbContext scoped (para repositorios). Usa PostgreSQL
        /// con convención de nombres snake_case.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <param name="configuration">Configuración de la aplicación. Se usa para leer la cadena de conexión PostgreSQL_HomeDB.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            //Obtener la cadena de conexión desde appsettings.json
            string connectionString = configuration.GetConnectionString("PostgreSQL_HomeDB")!;

            //Configurar opciones de base de datos desde appsettings.json y validar en tiempo de arranque
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection("ConnectionStrings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            //Este se usa por el logger, que es singleton, y necesita un DbContextFactory para crear instancias de AppDbContext
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention());

            //Este se usa para inyectar AppDbContext en los repositorios, que son scoped, y no necesitan un DbContextFactory
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention());

            return services;
        }
    }
}