using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Repositories;

namespace HomeDB.DependencyInjection
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Registra la infraestructura de logging: LogEntryRepository, LogBackgroundService
        /// como hosted service y el Logger singleton que encola entradas de forma asíncrona.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddLoggingInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ILogEntryRepository, LogEntryRepository>();
            services.AddSingleton<LogBackgroundService>();

            services.AddSingleton<ILogQueue>(sp =>
                sp.GetRequiredService<LogBackgroundService>());

            services.AddHostedService(sp =>
                sp.GetRequiredService<LogBackgroundService>());

            //Registrar Logger singleton (depende de LogEntryRepository + ILogQueue)
            services.AddSingleton<Logger>(sp =>
            {
                ILogEntryRepository repo = sp.GetRequiredService<ILogEntryRepository>();
                ILogQueue logQueue = sp.GetRequiredService<ILogQueue>();
                return new Logger(repo, logQueue);
            });

            return services;
        }
    }
}