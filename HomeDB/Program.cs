using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Repositories;
using HomeDB.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --------------------------- Connection string --------------------------- //
//TODO Agregar DefaultEncrypted en appsettings.json 
string connectionString = builder.Configuration.GetConnectionString("DefaultEncrypted")!; 

// --------------------------- Logging y BackgroundService --------------------------- //
builder.Services.AddSingleton<LogEntryRepository>(provider => new LogEntryRepository(connectionString));

builder.Services.AddSingleton<LogBackgroundService>();

builder.Services.AddSingleton<ILogQueue>(sp => 
    sp.GetRequiredService<LogBackgroundService>());

builder.Services.AddHostedService(sp => 
    sp.GetRequiredService<LogBackgroundService>());

// Registrar Logger singleton (depende de LogEntryRepository + ILogQueue)
builder.Services.AddSingleton<Logger>(sp =>
{
    LogEntryRepository repo = sp.GetRequiredService<LogEntryRepository>();
    ILogQueue logQueue = sp.GetRequiredService<ILogQueue>();
    return new Logger(repo, logQueue);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Middleware global de manejo de excepciones
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();