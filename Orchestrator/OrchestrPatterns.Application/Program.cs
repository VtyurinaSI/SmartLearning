using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrchestrPatterns.Application;
using OrchestrPatterns.Domain;
using Quartz;
using SmartLearning.Contracts;
using GatewayPatterns.Infrastructure;
using OrchestrPatterns.Application.Consumers;

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

builder.Services.AddSingleton<CompletionHub>();

builder.Services.AddObjectStorage(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReviewFinishedConsumer>();
    x.AddConsumer<ReviewFailedConsumer>();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}


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

app.MapPost("/mq", async (IBus bus,
                          CompletionHub hub,
                          GatewayPatterns.Infrastructure.IObjectStorageRepository repo,
                          StartMqDto dto,
                          CancellationToken ct) =>
{
    var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;

    if (dto.SkipCompile && dto.SkipTests)
        await bus.Publish(new StartReview(id), ct);
    else if (dto.SkipCompile)
        await bus.Publish(new StartTests(id), ct);
    else
        await bus.Publish(new StartCompile(id), ct);

    // ждём ReviewFinished/ReviewFailed из LlmService
    var ok = await hub.WaitAsync(id, TimeSpan.FromMinutes(5), ct);
    if (!ok) return Results.StatusCode(StatusCodes.Status504GatewayTimeout);

    // читаем сохранённый результат LLM и возвращаем гейту
    var review = await repo.ReadReviewAsync(id, ct);
    return review is null ? Results.NoContent() : Results.Ok(review);
});

app.Run();
