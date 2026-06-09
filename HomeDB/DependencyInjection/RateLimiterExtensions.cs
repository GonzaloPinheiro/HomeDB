using HomeDB.Common;
using HomeDB.Domain.Common;
using System.Threading.RateLimiting;

namespace HomeDB.DependencyInjection
{
    public static class RateLimiterExtensions
    {
        /// <summary>
        /// Registra las políticas de rate limiting: global (100 req/min por IP)
        /// y auth (10 req/min por IP) para frenar ataques de fuerza bruta.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        public static IServiceCollection AddRateLimiterConfiguration(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Global: 100 req/min por IP
                options.AddPolicy(nameof(RateLimiterNames.Global), context =>
                {
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"ip:{ip}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 100,
                            TokensPerPeriod = 100,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                // Auth: 10 req/min por IP — freno de fuerza bruta
                options.AddPolicy(nameof(RateLimiterNames.Auth), context =>
                {
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"auth:{ip}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                options.OnRejected = async (context, ct) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.Headers.RetryAfter = "60";
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        ApiObjResponse<object>.Failure(
                            ApiErrorCodes.RateLimitExceeded,
                            "Too many requests. Please try again later."), ct);
                };
            });

            return services;
        }
    }
}