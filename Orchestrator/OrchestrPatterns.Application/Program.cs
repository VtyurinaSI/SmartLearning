using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrchestrPatterns.Application;
using OrchestrPatterns.Domain;

var builder = WebApplication.CreateBuilder();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("compiler", c => c.BaseAddress = new Uri(
    Environment.GetEnvironmentVariable("COMPILER_URL") ?? "http://localhost:6006"));
builder.Services.AddHttpClient("checker", c => c.BaseAddress = new Uri(
    Environment.GetEnvironmentVariable("CHECKER_URL") ?? "http://localhost:6005"));
builder.Services.AddHttpClient("reviewer", c => c.BaseAddress = new Uri("http://localhost:6003/"));
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<CheckingStateMachineMt, CheckingSaga>()
     .InMemoryRepository(); 

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("localhost", "/", h => { h.Username("guest"); h.Password("guest"); });
        cfg.ConfigureEndpoints(ctx);
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapPost("/workflows", (WorkflowRequest req, IServiceProvider sp, CancellationToken ct) =>
{
    var router = ActivatorUtilities.CreateInstance<SimpleRouter>(sp, req.Content);
    Checking fsmRef = new();
    fsmRef.Start((tr, from, to) => router.HandleAsync(tr, from, to, fsmRef, CancellationToken.None).Wait());

    return fsmRef.Status.ToString()+$". LLM: {router.LlmAnswer}";
});
app.Run(/*"http://localhost:6004"*/);
public record WorkflowRequest(string Content);