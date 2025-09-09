using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrchestrPatterns.Application;
using OrchestrPatterns.Domain;
using SmartLearning.Contracts;
using OrchestrPatterns.Application.Consumers;
using MinIoStub;

var builder = WebApplication.CreateBuilder();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CompletionHub>();

builder.Services.AddObjectStorage(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReviewFinishedConsumer>();
    x.AddConsumer<ReviewFailedConsumer>();
    x.AddConsumer<CompileFinishedConsumers>();
    x.AddConsumer<CompileFailedConsumer>();
    x.SetKebabCaseEndpointNameFormatter();

    x.AddSagaStateMachine<CheckingStateMachineMt, CheckingSaga>()
        .InMemoryRepository();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.UseDelayedMessageScheduler();

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
var orc = app.MapGroup("/orc");

orc.MapPost("/check", async (IBus bus,
                          CompletionHub hub,
                          IObjectStorageRepository repo,
                          StartChecking dto,
                          CancellationToken ct) =>
{
    var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;


    await bus.Publish(new CompileRequested(id), ct);
    var ok = await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);
    await Task.Delay(200, ct);
    var compilRes = await repo.ReadCompilationAsync(id, ct);

    if (!ok)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, false, false, false), ct);
        return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, null, null));
    }
    Random rnd = new();
    if (rnd.Next(0, 2) == 0)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, false, false), ct);
        return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, null, null));
    }

    await bus.Publish(new ReviewRequested(id), ct);
    await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);
    var reviewRes = await repo.ReadReviewAsync(id, ct);
    await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, true, true), ct);
    return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, null, reviewRes));
});

app.Run();
