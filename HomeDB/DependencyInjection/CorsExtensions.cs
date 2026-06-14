using HomeDB.Common;

namespace HomeDB.DependencyInjection
{
    public static class CorsExtensions
    {
        /// <summary>
        /// Registra la política de CORS para el cliente de desarrollo frontend (localhost:5173).
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(nameof(CorsNames.AllowFrontend), policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); //Permite el envío de cookies y credenciales en solicitudes CORS
                });
            });

            return services;
        }
    }
}