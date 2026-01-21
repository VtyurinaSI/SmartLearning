using Gateway.SrvApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Serilog;
using SmartLearning.Contracts;
using System.Net;

namespace Gateway
{
    public static class WebApplicationExtensions
    {
        public static void UseGatewaySwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "swagger";
            });
        }

        public static void UseGatewayRequestLogging(this WebApplication app)
        {
            app.UseSerilogRequestLogging(opts =>
            {
                opts.IncludeQueryInRequestPath = true;
            });
        }

        public static void UseGatewayCorrelationId(this WebApplication app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        public static void UseGatewayUserIdHeader(this WebApplication app)
        {
            app.UseMiddleware<UserIdHeaderMiddleware>();
        }

        public static void MapGatewayEndpoints(this WebApplication app)
        {
            var api = app.MapGroup("/api");

            api.MapGet("/patterns/tasks", async (PatternsApi patterns, CancellationToken ct) =>
            {
                using var resp = await patterns.GetTasksAsync(ct);
                return await GatewayProxy.ProxyAsync(resp, ct);
            })
                .WithSummary("Available tasks");

            api.MapGet("/patterns/task_materials", async (long taskId, PatternsApi patterns, CancellationToken ct) =>
            {
                using var theoryResp = await patterns.GetTheoryAsync(taskId, ct);
                if (theoryResp.StatusCode == HttpStatusCode.NotFound)
                    return Results.NotFound($"Task {taskId} not found.");
                if (!theoryResp.IsSuccessStatusCode)
                    return await GatewayProxy.ProxyAsync(theoryResp, ct);

                using var taskResp = await patterns.GetTaskAsync(taskId, ct);
                if (taskResp.StatusCode == HttpStatusCode.NotFound)
                    return Results.NotFound($"Task {taskId} not found.");
                if (!taskResp.IsSuccessStatusCode)
                    return await GatewayProxy.ProxyAsync(taskResp, ct);

                var theory = await theoryResp.Content.ReadAsStringAsync(ct);
                var task = await taskResp.Content.ReadAsStringAsync(ct);
                return Results.Json(new TaskMaterials(theory, task));
            })
                .WithSummary("Task theory and assignment");

            api.MapGet("/progress/user_progress", async (HttpContext ctx, ProgressApi pr, CancellationToken ct) =>
            {
                if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
                    return Results.Unauthorized();
                using var resp = await pr.GetUserProgressAsync(userId, ct);
                return await GatewayProxy.ProxyAsync(resp, ct);
            })
                .RequireAuthorization()
                .WithSummary("Requesting user progress");

            api.MapGet("/users/me", async (UsersApi users, CancellationToken ct) =>
            {
                using var resp = await users.GetMeAsync(ct);
                return await GatewayProxy.ProxyAsync(resp, ct);
            })
                .RequireAuthorization()
                .WithSummary("Current user profile");

            api.MapPost("/orc/check", async (
                [FromQuery] long taskId,
                IFormFile file,
                HttpContext ctx,
                ProgressApi pr,
                OrchApi orc,
                GatewayObjectStorageClient minioHandler,
                PatternsApi patterns,
                CancellationToken ct) =>
            {
                if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
                    return Results.Unauthorized();
                Guid checkingId = Guid.NewGuid();
                await using var stream = file.OpenReadStream();
                await minioHandler.WriteFile(stream, file.FileName, userId, taskId, "load", ct);

                using var titleResp = await patterns.GetTaskTitleAsync(taskId, ct);
                if (titleResp.StatusCode == HttpStatusCode.NotFound)
                    return Results.NotFound($"Task {taskId} not found.");
                if (!titleResp.IsSuccessStatusCode)
                    return await GatewayProxy.ProxyAsync(titleResp, ct);

                var taskName = await titleResp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(taskName))
                    taskName = $"task {taskId}";

                using var resp = await orc.StartCheckAsync(new StartChecking(checkingId, userId, taskId, taskName), ct);
                if (!resp.IsSuccessStatusCode)
                    return await GatewayProxy.ProxyAsync(resp, ct);

                return Results.Accepted($"/api/orc/check/{checkingId}", new { correlationId = checkingId });
            })
                .DisableAntiforgery()
                .RequireAuthorization()
                .WithSummary("Code checking");

            api.MapPost("/auth/register", async ([FromBody] RegisterRequest req, AuthApi auth, CancellationToken ct) =>
            {
                using var resp = await auth.RegisterAsync(req, ct);
                return await GatewayProxy.ProxyAsync(resp, ct);
            })
                .AllowAnonymous()
            .WithSummary("Registering a new user");

            api.MapPost("/auth/login", async ([FromBody] LoginRequest req, AuthApi auth, CancellationToken ct) =>
            {
                using var resp = await auth.LoginAsync(req, ct);
                return await GatewayProxy.ProxyAsync(resp, ct);
            })
                .AllowAnonymous()
                .WithSummary("Authorization");

            app.MapHealthChecks("/health/ready");
        }

        public static void UseGatewayUiFiles(this WebApplication app)
        {
            var webRoot = app.Environment.WebRootPath
                          ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uiRoot = Path.Combine(webRoot, "ui");
            Directory.CreateDirectory(uiRoot);

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = "/ui",
                FileProvider = new PhysicalFileProvider(uiRoot),
                EnableDefaultFiles = true
            });
        }
    }
}

