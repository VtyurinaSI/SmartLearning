using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchestrPatterns.Application.Consumers;
using OrchestrPatterns.Domain;

namespace OrchestrPatterns.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrchestratorOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            services.AddOptions<DownstreamOptions>().Bind(configuration.GetSection("Downstream"));
            services.AddOptions<CheckTimeoutOptions>().Bind(configuration.GetSection("CheckTimeouts"));
            services.AddOptions<ReviewStorageOptions>().Bind(configuration.GetSection("ReviewStorage"));
            return services;
        }

        public static IServiceCollection AddOrchestratorHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();

            services.AddHttpClient("MinioStorage", c =>
                c.BaseAddress = new Uri(downstream.Storage));
            services.AddHttpClient<PatternServiceClient>(c =>
                c.BaseAddress = new Uri(downstream.Patterns));

            return services;
        }

        public static IServiceCollection AddOrchestratorHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks();
            return services;
        }

        public static IServiceCollection AddOrchestratorSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddOrchestratorCore(this IServiceCollection services)
        {
            services.AddSingleton<CompletionHub>();
            services.AddScoped<CheckRequestHandler>();
            services.AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);
            return services;
        }

        public static IServiceCollection AddOrchestratorMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<ReviewFinishedConsumer>();
                x.AddConsumer<ReviewFailedConsumer>();
                x.AddConsumer<CompileFinishedConsumers>();
                x.AddConsumer<CompileFailedConsumer>();
                x.AddConsumer<TestsFinishedConsumer>();
                x.AddConsumer<TestsFailedConsumer>();
                x.SetKebabCaseEndpointNameFormatter();

                x.AddSagaStateMachine<CheckingStateMachineMt, CheckingSaga>()
                    .InMemoryRepository();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.UserName);
                        h.Password(options.Password);
                    });
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
