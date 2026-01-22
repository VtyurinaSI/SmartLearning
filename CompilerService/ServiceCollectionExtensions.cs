using CompilerService.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompilerService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCompilerServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            services.AddOptions<DownstreamOptions>().Bind(configuration.GetSection("Downstream"));
            return services;
        }

        public static IServiceCollection AddCompilerServiceSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddCompilerServiceHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();
            services.AddHttpClient<IStageStorageClient, StageStorageClient>(c =>
                c.BaseAddress = new Uri(downstream.Storage));
            return services;
        }

        public static IServiceCollection AddCompilerServiceCore(this IServiceCollection services)
        {
            services.AddTransient<SourceLoadService>();
            services.AddTransient<BuildTargetLocator>();
            services.AddTransient<DotnetRunner>();
            services.AddTransient<BuildOutputUploader>();
            services.AddTransient<WorkDirCleaner>();
            services.AddTransient<CsprojParser>();
            services.AddTransient<DependensyChecker>();
            return services;
        }

        public static IServiceCollection AddCompilerServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<CompileRequestedConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.UserName);
                        h.Password(options.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}

