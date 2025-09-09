//USERSERVICE IMITATION
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ProgressService;
using UserSvcStub;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddUserProgressDb(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<UserCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();
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
    echo += ", GateWay [by users-svc]!";
    Console.WriteLine($"[users-svc] modified: {echo}");
    return Results.Json(new { svc = "users", got = echo });
});
app.Run("http://localhost:6001");