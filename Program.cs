using HISWEBAPI.Interface;
using HISWEBAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using log4net;
using log4net.Config;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logFolder = Path.Combine(AppContext.BaseDirectory, "Logs");
if (!Directory.Exists(logFolder))
    Directory.CreateDirectory(logFolder);

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
var logger = LogManager.GetLogger(typeof(Program));

logger.Info("Application starting...");

// CORS setup
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add repository
builder.Services.AddScoped<IHomeRepository, HomeRepository>();

// Add Redis cache
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // Your Redis server
});

// Add controllers
builder.Services.AddControllers();

// Swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

logger.Info("Application built successfully");

// Middleware configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

// Map controllers
app.MapControllers();

try
{
    logger.Info("Starting app...");
    app.Run();
}
catch (Exception ex)
{
    logger.Error("Application failed to start", ex);
    throw;
}
