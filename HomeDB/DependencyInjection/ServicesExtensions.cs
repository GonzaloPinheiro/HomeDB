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
        /// <param name="storageOptions">Opciones de almacenamiento.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, StorageOptions storageOptions)
        {
            // Repositorios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IFileItemRepository, FileItemRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogEntryRepository>();

            // Storage
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

            // Límite de tamaño de fichero
            services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = storageOptions.MaxFileSizeBytes!.Value);

            return services;
        }
    }
}