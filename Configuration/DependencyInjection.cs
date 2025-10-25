using HISWEBAPI.Interface;
using HISWEBAPI.Repositories;
using PMS.DAL;

namespace HISWEBAPI.Configuration
{
    public static class DependencyInjection
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Data Access Layer
            services.AddScoped<ICustomSqlHelper, CustomSqlHelper>();

            // Repositories
            services.AddScoped<IHomeRepository, HomeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Add more services here as your application grows
            // services.AddScoped<IProductRepository, ProductRepository>();
            // services.AddScoped<IOrderRepository, OrderRepository>();
        }

        public static void RegisterCaching(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";
            });
        }

        public static void RegisterCors(IServiceCollection services, string policyName)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: policyName,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });
        }
    }
}