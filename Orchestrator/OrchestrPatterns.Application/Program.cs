using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using OrchestrPatterns.Application;
using OrchestrPatterns.Domain;
using Quartz;
using SmartLearning.Contracts;

var builder = WebApplication.CreateBuilder();

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

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

app.MapPost("/mq", async (IBus bus, StartDto dto) =>
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

app.MapPost("/workflows", (WorkflowRequest req, IServiceProvider sp, CancellationToken ct) =>
{
    var router = ActivatorUtilities.CreateInstance<SimpleRouter>(sp, req.Content);
    Checking fsmRef = new();
    fsmRef.Start((tr, from, to) => router.HandleAsync(tr, from, to, fsmRef, CancellationToken.None).Wait());

    return fsmRef.Status.ToString() + $". LLM: {router.LlmAnswer}";
});

app.Run();

public record WorkflowRequest(string Content);
public record StartDto(bool SkipCompile = false, bool SkipTests = false, Guid CorrelationId = default);
