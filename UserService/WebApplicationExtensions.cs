using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace UserService
{
    public static class WebApplicationExtensions
    {
        public static async Task EnsureUserDbAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var bootstrapper = scope.ServiceProvider.GetRequiredService<IDbBootstrapper>();
            await bootstrapper.EnsureAsync();
        }

        public static void MapUserServiceEndpoints(this WebApplication app)
        {
            app.MapControllers();
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapGet("/ping", (HttpContext ctx) =>
            {
                var echo = ctx.Request.Headers["X-Echo"].FirstOrDefault();
                Console.WriteLine($"[users-svc] received: {echo}");
                echo += ", Gateway [by users-svc]!";
                Console.WriteLine($"[users-svc] modified: {echo}");
                return Results.Json(new { svc = "users", got = echo });
            });
        }
    }
}

