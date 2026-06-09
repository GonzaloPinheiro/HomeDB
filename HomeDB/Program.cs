using HomeDB.Common;
using HomeDB.DependencyInjection;
using HomeDB.Infrastructure.Data;
using HomeDB.Infrastructure.Storage;
using HomeDB.Middlewares;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------------- Configuraciónes de appsettings --------------------------- //
StorageOptions storageOptions = builder.Configuration.GetSection("Storage").Get<StorageOptions>()!;

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//---------------------------CORS---------------------------//
builder.Services.AddCorsConfiguration();

//---------------------------RateLimiter---------------------------//
builder.Services.AddRateLimiterConfiguration();

//---------------------------JWT Authentication---------------------------//    
builder.Services.AddJwtAuthentication(builder.Configuration);

// --------------------------- DbContext --------------------------- //
builder.Services.AddDatabase(builder.Configuration);

// --------------------------- Logging y BackgroundService --------------------------- //
builder.Services.AddLoggingInfrastructure();

// --------------------------- Services & Repositories --------------------------- //
builder.Services.AddApplicationServices(builder.Configuration, storageOptions);

// --------------------------- Health Checks --------------------------- //
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL_HomeDB")!)
    .AddDiskStorageHealthCheck(options =>
        options.AddDrive("/storage", minimumFreeMegabytes: 512),
        name: "storage");

// --------------------------- Límite de tamaño de fichero --------------------------- //
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = storageOptions.MaxFileSizeBytes);

// --------------------------- Construir aplicación --------------------------- //
var app = builder.Build();

// --------------------------- Middlewares --------------------------- //
//Middleware global de manejo de excepciones
app.UseMiddleware<ExceptionHandlerMiddleware>();
// Middleware que añade cabeceras de seguridad HTTP a todas las respuestas
app.UseMiddleware<SecurityHeadersMiddleware>();

// --------------------------- Pipeline HTTP --------------------------- //
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

//Permite el acceso desde el cliente frontend (localhost:5173) con CORS y envío de cookies
app.UseCors(nameof(CorsNames.AllowFrontend));

//Activa el rate limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


// --------------------------- Migraciones automáticas --------------------------- //
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --------------------------- Health Checks --------------------------- //
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();