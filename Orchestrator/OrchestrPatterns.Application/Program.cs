using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrchestrPatterns.Application;
using OrchestrPatterns.Domain;
using Quartz;
using SmartLearning.Contracts;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder();

builder.Services.AddHealthChecks();

#if DEBUG
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endif

builder.Services.AddHttpClient("compiler", c => c.BaseAddress =
    new Uri(Environment.GetEnvironmentVariable("COMPILER_URL") ?? "http://localhost:6006"));
builder.Services.AddHttpClient("checker", c => c.BaseAddress =
    new Uri(Environment.GetEnvironmentVariable("CHECKER_URL") ?? "http://localhost:6005"));


builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
});
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddSagaStateMachine<CheckingStateMachineMt, CheckingSaga>()
        .InMemoryRepository();

    x.AddMessageScheduler(new Uri("queue:quartz"));

    x.AddQuartzConsumers();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.UseMessageScheduler(new Uri("queue:quartz"));

        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);
var app = builder.Build();

#if DEBUG
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));
#endif

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

app.MapPost("/mq", async (IBus bus, StartMqDto dto) =>
{
    var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;

    if (dto.SkipCompile && dto.SkipTests)
        await bus.Publish(new StartReview(id));
    else if (dto.SkipCompile)
        await bus.Publish(new StartTests(id));
    else
        await bus.Publish(new StartCompile(id));

    return Results.Accepted($"/checking/{id}", new { id });
});

app.Run();
