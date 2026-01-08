using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;

namespace PatternService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPatternServiceOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CatalogOptions>(configuration.GetSection("Catalog"));
        return services;
    }

    public static IServiceCollection AddPatternServiceDb(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = PatternDbConnectionStrings.GetCatalog(configuration);
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));
        services.AddScoped<ITaskCatalogRepository, TaskCatalogRepository>();
        return services;
    }

    public static IServiceCollection AddPatternServiceStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IContentStorage, ContentStorage>(c =>
            c.BaseAddress = new Uri(configuration["Downstream:Storage"]!));
        return services;
    }

    public static IServiceCollection AddPatternServiceCore(this IServiceCollection services)
    {
        services.AddScoped<TaskCatalogService>();
        return services;
    }

    public static IServiceCollection AddPatternServiceSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}
