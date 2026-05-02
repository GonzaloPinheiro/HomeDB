using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces;
using HomeDB.Infrastructure.Data;
using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Repositories;
using HomeDB.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


//TODO Revisar la implementación, falta implemntar jwt.
builder.Services.AddRateLimiter(options =>
{
    // Global: 100 req/min por IP
    options.AddPolicy("global", context =>
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
    options.AddPolicy("auth", context =>
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



// --------------------------- Connection string --------------------------- //
//TODO Agregar DefaultEncrypted en appsettings.json
string connectionString = builder.Configuration.GetConnectionString("PostgreSQL_HomeDB")!;

// --------------------------- DbContext --------------------------- //
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// --------------------------- Logging y BackgroundService --------------------------- //
builder.Services.AddSingleton<ILogEntryRepository, LogEntryRepository>();

builder.Services.AddSingleton<LogBackgroundService>();

builder.Services.AddSingleton<ILogQueue>(sp => 
    sp.GetRequiredService<LogBackgroundService>());

builder.Services.AddHostedService(sp => 
    sp.GetRequiredService<LogBackgroundService>());

// Registrar Logger singleton (depende de LogEntryRepository + ILogQueue)
builder.Services.AddSingleton<Logger>(sp =>
{
    ILogEntryRepository repo = sp.GetRequiredService<ILogEntryRepository>();
    ILogQueue logQueue = sp.GetRequiredService<ILogQueue>();
    return new Logger(repo, logQueue);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Middleware global de manejo de excepciones
app.UseMiddleware<ExceptionHandlerMiddleware>();
// Middleware que añade cabeceras de seguridad HTTP a todas las respuestas
app.UseMiddleware<SecurityHeadersMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else//Fuerza a los navegadores a usar una conexión segura https(solo se aplica fuera de development)
{
    app.UseHsts();
}

//Redirige cualquier petición http a https
app.UseHttpsRedirection();

app.UseAuthorization();

//Activa el rate limiting
app.UseRateLimiter();

app.MapControllers();

app.Run();