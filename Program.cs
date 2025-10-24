using HISWEBAPI.Interface;
using HISWEBAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Core;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Read Logging Flags from appsettings.json
// ---------------------------
bool enableInfoLog = builder.Configuration.GetValue<bool>("LoggingFlags:EnableInfoLog");
bool enableWarnLog = builder.Configuration.GetValue<bool>("LoggingFlags:EnableWarnLog");
bool enableErrorLog = builder.Configuration.GetValue<bool>("LoggingFlags:EnableErrorLog");

// ---------------------------
// Configure log4net
// ---------------------------
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
var logger = LogManager.GetLogger(typeof(Program));

// Adjust root logger level dynamically based on flags
if (logRepository is log4net.Repository.Hierarchy.Hierarchy hierarchy)
{
    var root = hierarchy.Root;

    // Default to OFF
    root.Level = Level.Off;

    // Determine highest level to enable based on flags
    if (enableInfoLog)
        root.Level = Level.Info;
    else if (enableWarnLog)
        root.Level = Level.Warn;
    else if (enableErrorLog)
        root.Level = Level.Error;

    hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
}


logger.Info("Application starting...");

// ---------------------------
// CORS setup
// ---------------------------
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

// ---------------------------
// Dependency Injection
// ---------------------------
builder.Services.AddScoped<IHomeRepository, HomeRepository>();

// ---------------------------
// Redis Cache
// ---------------------------
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // Redis server
});

// ---------------------------
// Controllers and Swagger
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

logger.Info("Application built successfully");

// ---------------------------
// Middleware
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

// ---------------------------
// Run Application
// ---------------------------
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
