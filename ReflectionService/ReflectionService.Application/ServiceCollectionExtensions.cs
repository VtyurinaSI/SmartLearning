using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReflectionService.Domain.PipelineOfCheck;
using ReflectionService.Domain.Reporting;

namespace ReflectionService.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReflectionServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            services.AddOptions<DownstreamOptions>().Bind(configuration.GetSection("Downstream"));
            return services;
        }

        public static IServiceCollection AddReflectionServiceSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddReflectionServiceHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();
            services.AddHttpClient<ReflectionRequestedConsumer>(c =>
                c.BaseAddress = new Uri(downstream.Storage));
            services.AddHttpClient<PatternServiceClient>(c =>
                c.BaseAddress = new Uri(downstream.Patterns));
            return services;
        }

        public static IServiceCollection AddReflectionServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<ReflectionRequestedConsumer>();
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

        public static IServiceCollection AddReflectionServicePipeline(this IServiceCollection services)
        {
            services.AddReflectionStepHandlers();
            services.AddTransient<CheckingPipeline>();
            services.AddTransient<ICheckingReportBuilder, CheckingReportBuilder>();
            return services;
        }
    }
}
