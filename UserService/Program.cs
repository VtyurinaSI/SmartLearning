//USERSERVICE IMITATION
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using ProgressService;
using System.Data;
using UserSvcStub;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddUserProgressDb(builder.Configuration);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? builder.Configuration.GetConnectionString("ObjectStorage");
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<UserCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var mq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(mq["Host"] ?? "rabbitmq", mq["VirtualHost"] ?? "/", h =>
        {
            h.Username(mq["UserName"] ?? "guest");
            h.Password(mq["Password"] ?? "guest");
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
app.Run();