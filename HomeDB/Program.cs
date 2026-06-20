using HomeDB.Common;
using HomeDB.DependencyInjection;
using HomeDB.Infrastructure.Data;
using HomeDB.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//---------------------------CORS---------------------------//
builder.Services.AddCorsConfiguration(builder.Configuration);

//---------------------------RateLimiter---------------------------//
builder.Services.AddRateLimiterConfiguration();

//---------------------------SystemMonitoring---------------------------//
builder.Services.AddSystemMonitoring(builder.Configuration);

//---------------------------JWT Authentication---------------------------//    
builder.Services.AddJwtAuthentication(builder.Configuration);

// --------------------------- DbContext --------------------------- //
builder.Services.AddDatabase(builder.Configuration);

// --------------------------- Logging y BackgroundService --------------------------- //
builder.Services.AddLoggingInfrastructure();

// --------------------------- Services & Repositories --------------------------- //
builder.Services.AddApplicationServices(builder.Configuration);

// --------------------------- Health Checks --------------------------- //
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL_HomeDB")!)
    .AddDiskStorageHealthCheck(options =>
        options.AddDrive("/storage", minimumFreeMegabytes: 512),
        name: "storage");

// --------------------------- Límite de tamaño de fichero --------------------------- //
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = builder.Configuration.GetValue<long?>("Storage:MaxFileSizeBytes"));

// TODO: mover a un Options class con ValidateOnStart siguiendo el patrón del resto de opciones
string allowedHosts = builder.Configuration["AllowedHosts"] ?? string.Empty;
if (!builder.Environment.IsDevelopment() && allowedHosts is "*" or "AllowedHostsPlaceHolder" or "")
    throw new InvalidOperationException("AllowedHosts debe configurarse con un hostname real.");

// --------------------------- Construir aplicación --------------------------- //
var app = builder.Build();

//--------------------------- Middleware para encabezados reenviados --------------------------- //
//Configura la aplicación para procesar la ip y esquema real del cliente correctamente, ya que la api está detrás de cloudflared.
//cloudflared llega al contenedor desde la red bridge de Docker (no loopback), por lo que hay que declararla
//como red de confianza; si no, el middleware descarta X-Forwarded-For/X-Forwarded-Proto en silencio.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12) } //Rango privado que usa Docker para sus redes bridge
});

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