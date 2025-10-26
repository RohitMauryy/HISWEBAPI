using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Repositories.Implementations;
using HISWEBAPI.Repositories.Interfaces;

namespace HISWEBAPI.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // CORS Configuration
        RegisterCors(services, configuration);

        // Data Access Layer
        RegisterDataAccess(services);

        // Repositories
        RegisterRepositories(services);

        // Business Services
        RegisterBusinessServices(services);

        // Caching
        RegisterCaching(services, configuration);

        return services;
    }

    private static void RegisterCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("_myAllowSpecificOrigins", policy =>
            {
                policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()
                                   ?? new[] { "http://localhost:5173" })
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    private static void RegisterDataAccess(IServiceCollection services)
    {
        services.AddScoped<ICustomSqlHelper, CustomSqlHelper>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IHomeRepository, HomeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

    }

    private static void RegisterBusinessServices(IServiceCollection services)
    {
        // Add business services here
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IAuthService, AuthService>();
    }

    private static void RegisterCaching(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDistributedRedisCache(options =>
        {
            options.Configuration = configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";
        });

        // Also add memory cache as fallback
        services.AddMemoryCache();
    }
}