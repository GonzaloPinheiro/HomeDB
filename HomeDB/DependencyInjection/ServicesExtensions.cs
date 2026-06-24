using HomeDB.Application.Services;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Repositories;
using HomeDB.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Features;

namespace HomeDB.DependencyInjection
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Registra los repositorios, servicios de aplicación, servicios de almacenamiento
        /// en disco y el límite de tamaño de fichero para multipart uploads.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <param name="configuration">Configuración de la aplicación. Se usa para leer la sección Storage.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositorios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IFileItemRepository, FileItemRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogEntryRepository>();

            // Storage (+validation)
            services.AddOptions<StorageOptions>()
                    .Bind(configuration.GetSection("Storage"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            services.AddScoped<IFileStorageService, FileStorageService>();

            // Servicios de aplicación
            services.AddScoped<AuthService>();
            services.AddScoped<FilesService>();
            services.AddScoped<FoldersService>();
            services.AddScoped<AuditService>();
            services.AddScoped<StatisticsService>();
            services.AddScoped<UsersService>();

            // Límite de tamaño de fichero
            services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = configuration.GetValue<long>("Storage:MaxFileSizeBytes"));

            return services;
        }
    }
}