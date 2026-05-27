using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HomeDB.DependencyInjection
{
    public static class AuthExtensions
    {
        /// <summary>
        /// Registra la autenticación JWT, los helpers de seguridad y los servicios
        /// relacionados con el usuario autenticado (IPasswordHelper, IJwtService, ICurrentUserService).
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <param name="configuration">Configuración de la aplicación. Se usa para leer Jwt:Issuer y Jwt:Key.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            //Configurar opciones de JWT desde appsettings.json y validar en tiempo de arranque
            services.AddOptions<JwtOptions>()
             .Bind(configuration.GetSection("Jwt"))
             .ValidateDataAnnotations()
             .ValidateOnStart();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                    };
                });

            services.AddScoped<IPasswordHelper, PasswordHelper>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

    }
}