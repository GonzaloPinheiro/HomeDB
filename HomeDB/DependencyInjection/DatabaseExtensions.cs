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

            // Registra IDbContextFactory<AppDbContext> como Singleton para el LogEntryRepository.
            // Una sola configuración: evita que EF Core acumule dos IDbContextOptionsConfiguration<T>
            // y llame a UseNpgsql dos veces, lo que corrompía la resolución de IMigrationsAssembly.
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                           npgsql.MigrationsAssembly("HomeDB.Infrastructure"))
                       .UseSnakeCaseNamingConvention());

            // Registra AppDbContext como Scoped para los repositorios, delegando en la factory.
            // No usa AddDbContext para no registrar una segunda IDbContextOptionsConfiguration<T>.
            services.AddScoped<AppDbContext>(sp =>
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            return services;
        }
    }
}