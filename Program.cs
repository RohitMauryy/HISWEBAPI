using HISWEBAPI.Configuration;
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
string logBasePath = builder.Configuration.GetValue<string>("LoggingFlags:LogBasePath") ?? "Logs/";
GlobalContext.Properties["LogPath"] = logBasePath;

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
var logger = LogManager.GetLogger(typeof(Program));

if (logRepository is log4net.Repository.Hierarchy.Hierarchy hierarchy)
{
    var root = hierarchy.Root;
    root.Level = Level.Off;

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
// Service Registration
// ---------------------------
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

DependencyInjection.RegisterCors(builder.Services, MyAllowSpecificOrigins);
DependencyInjection.RegisterServices(builder.Services, builder.Configuration);
DependencyInjection.RegisterCaching(builder.Services, builder.Configuration);

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

    app.UseSwagger();
    app.UseSwaggerUI();


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