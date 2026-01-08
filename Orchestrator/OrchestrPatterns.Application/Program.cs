using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrchestrPatterns.Application;
using OrchestrPatterns.Application.Consumers;
using OrchestrPatterns.Domain;
using Serilog;
using Serilog.Events;
using SmartLearning.Contracts;
using System.Data;
using System.Text;
Console.OutputEncoding = System.Text.Encoding.UTF8;
var builder = WebApplication.CreateBuilder();
builder.Host.UseSerilog((ctx, lc) =>
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .MinimumLevel.Debug()
          .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
          .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
          .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
          .Enrich.FromLogContext()
          .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    });
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
var cs = builder.Configuration.GetConnectionString("ConnectionStrings")
         ?? builder.Configuration.GetConnectionString("ObjectStorage");

builder.Services.AddHttpClient("MinioStorage", c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));
builder.Services.AddHttpClient<PatternServiceClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Patterns"]!));

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CompletionHub>();


builder.Services.AddMassTransit(x =>
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
        var mq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(mq["Host"] ?? "rabbitmq", mq["VirtualHost"] ?? "/", h =>
        {
            h.Username(mq["UserName"] ?? "guest");
            h.Password(mq["Password"] ?? "guest");
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
                          StartChecking dto,
                          IHttpClientFactory _http,
                          PatternServiceClient patterns,
                          ILogger<Program> log,
                          CancellationToken ct) =>
{
    var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;

    var exists = await patterns.TaskExistsAsync(dto.TaskId, ct);
    if (exists == false)
        return Results.NotFound($"Задача с id {dto.TaskId} не найдена.");
    if (exists is null)
        log.LogWarning("PatternService unavailable while checking task {TaskId}", dto.TaskId);

    await bus.Publish(new CompileRequested(id, dto.UserId, dto.TaskId), ct);

    var (okCompile, compilRes) = await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);
    await Task.Delay(200, ct);

    if (!okCompile)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, false, false, false, id, false, compilRes, null, null), ct);
        var progressFailCompile = new UserProgressRow(dto.UserId, dto.TaskId, "task " + dto.TaskId.ToString(), id,
            false,
            false, compilRes,
            false, null,
            false, null);
        return Results.Ok(progressFailCompile);
    }

    await bus.Publish(new TestRequested(id, dto.UserId, dto.TaskId), ct);

    var (okReflection, testRes) = await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);

    if (!okReflection)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, false, false, id, false, compilRes, testRes, null), ct);
        return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, "task " + dto.TaskId.ToString(), id,
            false,
            true, compilRes,
            false, testRes,
            false, null));
    }

    var patternName = await patterns.GetPatternTitleAsync(dto.TaskId, ct);
    if (string.IsNullOrWhiteSpace(patternName))
        patternName = string.Empty;
    await bus.Publish(new ReviewRequested(id, dto.UserId, dto.TaskId, patternName), ct);
    var (okReview, reviewRes) = await hub.WaitAsync(id, TimeSpan.FromMinutes(10), ct);

    if (!okReview)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, true, false, id, false, compilRes, testRes, reviewRes), ct);
        return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, "task " + dto.TaskId.ToString(), id,
            false,
            true, compilRes,
            true, testRes,
            false, reviewRes));
    }

    var minioClient = _http.CreateClient("MinioStorage");
    var url = $"/objects/llm/file?userId={dto.UserId}&taskId={dto.TaskId}&fileName={"review.txt"}";

    try
    {
        using var respMinio = await minioClient.GetAsync(url, ct);
        if (respMinio.IsSuccessStatusCode)
        {
            var bytes = await respMinio.Content.ReadAsByteArrayAsync(ct);
            reviewRes = Encoding.UTF8.GetString(bytes);
        }
    }
    catch (Exception ex)
    {
        log.LogDebug(ex, "Failed to read review from storage for {Cid}", id);
    }

    await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, true, true, id, true, compilRes, testRes, reviewRes), ct);
    return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, "task " + dto.TaskId.ToString(), id,
        true,
        true, compilRes,
        true, testRes,
        true, reviewRes));
});

app.Run();
