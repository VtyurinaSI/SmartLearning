using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GatewayPatterns.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("ObjectStorage")
                     ?? throw new InvalidOperationException("ConnectionStrings:ObjectStorage is not configured");

            services.AddSingleton(_ => NpgsqlDataSource.Create(cs));
            services.AddScoped<IObjectStorageRepository, ObjectStorageRepository>();
            return services;
        }
    }
}
