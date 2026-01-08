using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;

namespace ProgressService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUserProgressDb(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = ProgressDbConnectionStrings.GetObjectStorage(cfg);

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));
            services.AddSingleton(_ => NpgsqlDataSource.Create(cs));
            services.AddScoped<IUserProgressRepository, UserProgressRepository>();
            return services;
        }
    }
}
