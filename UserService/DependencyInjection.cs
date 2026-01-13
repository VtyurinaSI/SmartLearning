using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;

namespace UserService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUserProgressDb(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = UserDbConnectionStrings.GetObjectStorage(cfg);
            var dapperCs = UserDbConnectionStrings.GetDefaultOrObjectStorage(cfg);

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(dapperCs));
            services.AddSingleton(_ => NpgsqlDataSource.Create(cs));
            services.AddScoped<IUserProgressRepository, UserProgressRepository>();
            services.AddSingleton<IDbBootstrapper, DbBootstrapper>();
            return services;
        }
    }
}

