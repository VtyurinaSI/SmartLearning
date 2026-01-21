using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Orchestrator.Application.Services;
using SmartLearning.Contracts;

namespace Orchestrator.Application
{
    public static class WebApplicationExtensions
    {
        public static void UseOrchestratorSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        public static void MapOrchestratorEndpoints(this WebApplication app)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            var orc = app.MapGroup("/orc");
            orc.MapPost("/check", async (CheckRequestHandler handler, StartCheckRequest dto, CancellationToken ct) =>
                await handler.HandleAsync(dto, ct));
        }
    }
}

