using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Data;
using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Repositories;
using HomeDB.Infrastructure.Security;
using HomeDB.Infrastructure.Storage;
using HomeDB.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//---------------------------CORS---------------------------//
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


//---------------------------RateLimiter---------------------------//
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

//---------------------------JWT Authentication---------------------------//
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// --------------------------- Connection string --------------------------- //
string connectionString = builder.Configuration.GetConnectionString("PostgreSQL_HomeDB")!;

// --------------------------- DbContext --------------------------- //
//Este se usa por el logger, que es singleton, y necesita un DbContextFactory para crear instancias de AppDbContext
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

//Este se usa para inyectar AppDbContext en los repositorios, que son scoped, y no necesitan un DbContextFactory
builder.Services.AddDbContext<AppDbContext>(options =>
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

// --------------------------- Repositories --------------------------- //
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IFileItemRepository, FileItemRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// --------------------------- Storage --------------------------- //
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

//---------------------------HelpersSeguridad + JWT---------------------------//
builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
builder.Services.AddScoped<IJwtService, JwtService>();

// --------------------------- AuthService --------------------------- //
builder.Services.AddScoped<AuthService>();
// --------------------------- FilesService --------------------------- //
builder.Services.AddScoped<FilesService>();
// --------------------------- FoldersService --------------------------- //
builder.Services.AddScoped<FoldersService>();

// --------------------------- Límite de tamaño de fichero --------------------------- //
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 524288000);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 524288000);

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

app.UseCors("FrontendDev");

app.UseAuthentication();
app.UseAuthorization();

//Activa el rate limiting
app.UseRateLimiter();

app.MapControllers();

app.Run();