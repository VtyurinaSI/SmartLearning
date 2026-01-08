using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace LlmService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLlmServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection("RabbitMq"));
            services.AddOptions<DownstreamOptions>().Bind(configuration.GetSection("Downstream"));
            services.AddOptions<OllamaOptions>().Bind(configuration.GetSection("Ollama"));
            services.AddOptions<ReviewPromptOptions>().Bind(configuration.GetSection("ReviewPrompt"));
            return services;
        }

        public static IServiceCollection AddLlmServiceSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddLlmServiceHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var ollama = configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new OllamaOptions();
            var downstream = configuration.GetSection("Downstream").Get<DownstreamOptions>() ?? new DownstreamOptions();

            services.AddHttpClient("Ollama", client =>
            {
                client.BaseAddress = new Uri(ollama.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(ollama.TimeoutMinutes);
            });

            services.AddHttpClient("MinioStorage", c =>
                c.BaseAddress = new Uri(downstream.Storage));

            return services;
        }

        public static IServiceCollection AddLlmServiceMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<ReviewRequestedConsumer>();
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

        public static IServiceCollection AddLlmServiceCore(this IServiceCollection services)
        {
            services.AddTransient<IReviewStorageClient, ReviewStorageClient>();
            services.AddTransient<ReviewSourceLoader>();
            services.AddTransient<ReviewFileCollector>();
            services.AddTransient<ReviewProjectStructureBuilder>();
            services.AddTransient<ReviewPromptBuilder>();
            services.AddTransient<OllamaChatClient>();
            services.AddTransient<ReviewResultParser>();
            services.AddTransient<ReviewResponseFormatter>();
            services.AddTransient<WorkDirCleaner>();
            return services;
        }
    }
}
