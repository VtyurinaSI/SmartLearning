//USERSERVICE IMITATION
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
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