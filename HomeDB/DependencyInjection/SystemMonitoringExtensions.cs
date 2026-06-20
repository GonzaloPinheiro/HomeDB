using HomeDB.Application.Services;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Repositories;
using HomeDB.Infrastructure.SystemMonitoring;
using System.Runtime.InteropServices;

namespace HomeDB.DependencyInjection
{
    public static class SystemMonitoringExtensions
    {
        public static IServiceCollection AddSystemMonitoring(this IServiceCollection services, IConfiguration configuration)
        {
            //Selección de implementación según el sistema operativo en el que corre la API.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) //Linux
            {
                services.AddSingleton<ISystemMetricsReaderService, LinuxSystemMetricsReader>();
            }
            else //Otro sistema (Windows) //TODO Datos falsos de momento, falta crear reader de windows
            {
                services.AddSingleton<ISystemMetricsReaderService, FakeSystemMetricsReader>();
            }

            services.AddScoped<ISystemMetricsRepository, SystemMetricsRepository>();
            services.AddScoped<SystemMetricsService>();

            //Configurar opciones para la lectura de las métricas del sistema en el arrance de la api
            services.AddOptions<SystemMetricsOptions>()
                .Bind(configuration.GetSection("SystemMetrics"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHostedService<SystemMetricsBackgroundService>();

            return services;
        }
    }
}