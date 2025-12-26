using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MinIoStub;
using Npgsql;
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
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

    lc.MinimumLevel.Is(LogEventLevel.Debug);
});
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
var cs = builder.Configuration.GetConnectionString("ConnectionStrings")
         ?? builder.Configuration.GetConnectionString("ObjectStorage");

builder.Services.AddHttpClient("MinioStorage", c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));
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
    x.AddConsumer<TestsFinishedConsumer>();
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
                          IObjectStorageRepository repo,
                          StartChecking dto,
                          IHttpClientFactory _http,
                          ILogger<Program> log,
                          CancellationToken ct) =>
{
    var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;

    //await bus.Publish(new StartCompile(id, dto.UserId, dto.TaskId), ct);

    await bus.Publish(new CompileRequested(id, dto.UserId, dto.TaskId), ct);

    var ok = await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);
    await Task.Delay(200, ct);
    var compilRes = await repo.ReadCompilationAsync(id, ct);

    if (!ok)
    {
        await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, false, false, false), ct);
        return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, null, null));
    }
    
    await bus.Publish(new TestRequested(id, dto.UserId, dto.TaskId), ct);
    
    var okReflection = await hub.WaitAsync(id, TimeSpan.FromMinutes(2), ct);
    
    if (!okReflection)    
        return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, null, null));

    await bus.Publish(new ReviewRequested(id, dto.UserId, dto.TaskId), ct);
    await hub.WaitAsync(id, TimeSpan.FromMinutes(10), ct);
    var minioClient = _http.CreateClient("MinioStorage");
    var url = $"/objects/llm/file?userId={dto.UserId}&taskId={dto.TaskId}&fileName={"review.txt"}";

    using var respMinio = await minioClient.GetAsync(url);

    respMinio.EnsureSuccessStatusCode();

    var bytes = await respMinio.Content.ReadAsByteArrayAsync();
    var reviewRes = Encoding.UTF8.GetString(bytes);
    await bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, true, true, true), ct);
    return Results.Ok(new CheckingResults(dto.UserId, id, compilRes, "ok", reviewRes));
});

app.Run();
