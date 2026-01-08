using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using ProgressService;
using System.Data;
using System.Text;
using UserService;

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
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();
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
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IDbBootstrapper>();
    await bootstrapper.EnsureAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
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
await app.RunAsync();
