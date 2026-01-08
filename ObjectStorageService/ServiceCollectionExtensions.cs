using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace ObjectStorageService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddObjectStorageOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<StorageOptions>(configuration.GetSection("Storage"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<StorageOptions>>().Value);
            return services;
        }

        public static IServiceCollection AddObjectStorageMinio(this IServiceCollection services)
        {
            services.AddSingleton<IMinioClient>(sp =>
            {
                var opts = sp.GetRequiredService<StorageOptions>();
                var uri = new Uri(opts.Endpoint);
                var mcBuilder = new MinioClient()
                    .WithEndpoint(uri.Host, uri.Port)
                    .WithCredentials(opts.AccessKey, opts.SecretKey);
                if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    mcBuilder = mcBuilder.WithSSL();
                }

                return mcBuilder.Build();
            });
            services.AddSingleton<IStorageBootstrapper, StorageBootstrapper>();
            return services;
        }

        public static IServiceCollection AddObjectStorageSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddObjectStorageLogging(this IServiceCollection services)
        {
            var factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(opt =>
                {
                    opt.TimestampFormat = "HH:mm:ss.fff ";
                    opt.UseUtcTimestamp = true;
                    opt.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                    opt.SingleLine = true;
                }));

            services.AddSingleton(factory);
            services.AddSingleton(factory.CreateLogger<Program>());
            return services;
        }
    }
}
