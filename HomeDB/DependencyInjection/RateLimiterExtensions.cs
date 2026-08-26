using HomeDB.Application.Options;
using HomeDB.Common;
using HomeDB.Domain.Common;
using System.Threading.RateLimiting;

namespace HomeDB.DependencyInjection
{
    public static class RateLimiterExtensions
    {
        /// <summary>
        /// Registra las políticas de rate limiting: global y auth para frenar ataques de fuerza bruta.
        /// </summary>
        public static IServiceCollection AddRateLimiterConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            //Vincular la configuración de RateLimitingOptions desde appsettings.json
            services.AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            //Obtener la configuración de RateLimitingOptions
            RateLimitingOptions rateLimitingOptions = configuration
                .GetSection(RateLimitingOptions.SectionName)
                .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            services.AddRateLimiter(options =>
            {
                // Global: 100 req/min por IP
                options.AddPolicy(nameof(RateLimiterNames.Global), context =>
                {
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    string partitionKey = $"global:{ip}";

                    //Si la política de rate limiting para Global está deshabilitada, no aplicar limitación
                    if (!rateLimitingOptions.Global.Enabled)
                    {
                        return RateLimitPartition.GetNoLimiter(partitionKey);
                    }

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = rateLimitingOptions.Global.TokenLimit,
                            TokensPerPeriod = rateLimitingOptions.Global.TokensPerPeriod,
                            ReplenishmentPeriod = rateLimitingOptions.Global.ReplenishmentPeriod,
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });



                // Auth: 10 req/min por IP
                options.AddPolicy(nameof(RateLimiterNames.Auth), context =>
                {
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    string partitionKey = $"auth:{ip}";

                    //Si la política de rate limiting para Auth está deshabilitada, no aplicar limitación
                    if (!rateLimitingOptions.Auth.Enabled)
                    {
                        return RateLimitPartition.GetNoLimiter(partitionKey);
                    }

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: partitionKey,

                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = rateLimitingOptions.Auth.TokenLimit,
                            TokensPerPeriod = rateLimitingOptions.Auth.TokensPerPeriod,
                            ReplenishmentPeriod = rateLimitingOptions.Auth.ReplenishmentPeriod,
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