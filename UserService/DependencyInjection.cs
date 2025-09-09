using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ProgressService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUserProgressDb(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("ObjectStorage")
                     ?? throw new InvalidOperationException("ConnectionStrings:ObjectStorage is not configured");

            services.AddSingleton(_ => NpgsqlDataSource.Create(cs));
            services.AddScoped<IUserProgressRepository, UserProgressRepository>();
            return services;
        }
    }
}
