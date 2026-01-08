using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProgressService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProgressServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            return services;
        }

        public static IServiceCollection AddProgressServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<UpdateProgressConsumer>();
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

        public static IServiceCollection AddProgressServiceSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddProgressServiceCore(this IServiceCollection services)
        {
            services.AddTransient<ProgressUpdateService>();
            return services;
        }
    }
}
