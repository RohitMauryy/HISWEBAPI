using HISWEBAPI.Configuration;
using log4net;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.AddLoggingConfiguration();

// Add services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSwaggerConfiguration();
builder.Services.AddControllers();

// Build app
var app = builder.Build();
var logger = LogManager.GetLogger(typeof(Program));
logger.Info("Application starting...");

// Configure middleware pipeline
app.UseSwaggerConfiguration();
app.UseHttpsRedirection();
app.UseCors("_myAllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run application
try
{
    logger.Info("Application running on " + app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    logger.Fatal("Application terminated unexpectedly", ex);
    throw;
}
finally
{
    logger.Info("Application stopped");
    LogManager.Shutdown();
}