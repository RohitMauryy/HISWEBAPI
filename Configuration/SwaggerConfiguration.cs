namespace HISWEBAPI.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "HISWEBAPI",
                Version = "v1",
                Description = "Hospital Information System Web API"
            });

            // Add JWT Authentication to Swagger (if using JWT)
            // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
        });

        return services;
    }

    public static WebApplication UseSwaggerConfiguration(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "HISWEBAPI v1");
            // options.RoutePrefix = string.Empty; // REMOVE THIS or set to "swagger"
        });

        return app;
    }
}
