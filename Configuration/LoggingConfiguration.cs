using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Core;
using System.Reflection;
using log4net.Repository;

namespace HISWEBAPI.Configuration;

public static class LoggingConfiguration
{
    public static WebApplicationBuilder AddLoggingConfiguration(this WebApplicationBuilder builder)
    {
        string logBasePath = builder.Configuration.GetValue<string>("LoggingFlags:LogBasePath") ?? "Logs/";
        GlobalContext.Properties["LogPath"] = logBasePath;

        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        ConfigureLogLevel(
            logRepository,
            builder.Configuration.GetValue<bool>("LoggingFlags:EnableInfoLog"),
            builder.Configuration.GetValue<bool>("LoggingFlags:EnableWarnLog"),
            builder.Configuration.GetValue<bool>("LoggingFlags:EnableErrorLog")
        );

        return builder;
    }

    private static void ConfigureLogLevel(ILoggerRepository repository, bool enableInfo, bool enableWarn, bool enableError)
    {
        if (repository is Hierarchy hierarchy)
        {
            var root = hierarchy.Root;
            root.Level = Level.Off;

            if (enableInfo)
                root.Level = Level.Info;
            else if (enableWarn)
                root.Level = Level.Warn;
            else if (enableError)
                root.Level = Level.Error;

            hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
        }
    }
}