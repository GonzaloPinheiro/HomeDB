using HomeDB.Common;
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
                    //Configurar validación de tokens JWT
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                    };
                    //Configurar eventos para permitir la autenticación desde cookies si no se proporciona el header Authorization
                    options.Events = new JwtBearerEvents
                    {
                        //Este evento se ejecuta cuando el middleware de autenticación recibe una solicitud
                        OnMessageReceived = context =>
                        {
                            //Si ya viene el header Authorization, dejarlo pasar sin tocar nada
                            string? authHeader = context.Request.Headers["Authorization"]
                                .FirstOrDefault();
                            
                            if (!string.IsNullOrEmpty(authHeader))
                            {
                                return Task.CompletedTask;
                            }

                            // Si no hay header, intentar leer la cookie
                            string? tokenFromCookie = context.Request.Cookies[nameof(CookieNames.AccessToken)];
                            if (!string.IsNullOrEmpty(tokenFromCookie))
                            {
                                context.Token = tokenFromCookie;
                            }

                            return Task.CompletedTask;
                        }
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