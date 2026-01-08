using Serilog;
using Serilog.Events;

namespace ReflectionService.Application
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseReflectionServiceSerilog(this IHostBuilder host)
        {
            host.UseSerilog((ctx, lc) =>
            {
                lc.ReadFrom.Configuration(ctx.Configuration)
                  .MinimumLevel.Debug()
                  .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                  .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
                  .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                  .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
                  .MinimumLevel.Override("RabbitMQ.Client", LogEventLevel.Warning)
                  .Enrich.FromLogContext()
                  .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            });

            return host;
        }
    }
}
