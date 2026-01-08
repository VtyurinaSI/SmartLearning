using GatewayPatterns;
using GatewayPatterns.SrvApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SmartLearning.Contracts;
using System.Data;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
var cs = builder.Configuration.GetConnectionString("ObjectStorage");

builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(c =>
    {
        var jwtScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
    });
builder.Services.AddHeaderPropagation(o =>
{
    o.Headers.Add("Authorization");
    o.Headers.Add("X-Correlation-Id");
    o.Headers.Add("X-User-Id");
});
builder.Services.AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);
builder.Services.AddHttpClient<UsersApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Users"]!))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<ProgressApi>(c =>    
     c.BaseAddress = new Uri(builder.Configuration["Downstream:Progress"]!))        
    .AddHeaderPropagation();

builder.Services.AddHttpClient<OrchApi>(c =>
    { c.BaseAddress = new Uri(builder.Configuration["Downstream:Orch"]!);
    c.Timeout = TimeSpan.FromMinutes(10);
    }).AddHeaderPropagation();

builder.Services.AddHttpClient<AuthApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Auth"]!))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<PatternsApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Patterns"]!))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<GatewayObjectStorageClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!); 
});

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

builder.Services.AddHealthChecks()
    .AddCheck("gateway_self", () => HealthCheckResult.Healthy("OK"))
    .AddUrlGroup(new Uri($"{builder.Configuration["Downstream:Users"]}health/ready"), name: "users_svc")
    .AddUrlGroup(new Uri($"{builder.Configuration["Downstream:Llm"]}health/ready"), name: "llm_svc");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var key = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Key не задан в конфиге гейта.");

        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,   // временно 
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false, // временно 
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
#warning "ClockSkew с рефрешем"
            ClockSkew = TimeSpan.FromMinutes(10)
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
});


app.UseSerilogRequestLogging(opts =>
{
    opts.IncludeQueryInRequestPath = true;
});

app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Headers.ContainsKey("X-Correlation-Id"))
        ctx.Request.Headers["X-Correlation-Id"] = ctx.TraceIdentifier;

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", ctx.Request.Headers["X-Correlation-Id"].ToString()))
        await next();
});
app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    ctx.Request.Headers.Remove("X-User-Id");

    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var uid = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? ctx.User.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(uid))
            ctx.Request.Headers["X-User-Id"] = uid;
    }
    await next();
});
var api = app.MapGroup("/api");
api.MapPost("/file", (IFormFile file) =>
{

    return Results.Ok($"Получили {file.FileName}!");
})
   .DisableAntiforgery()
   .Produces(StatusCodes.Status200OK)
   .WithOpenApi();

api.MapGet("/ping", () =>
{
    return "pong";
});
api.MapGet("/patterns/tasks", async (PatternsApi patterns, CancellationToken ct) =>
{
    using var resp = await patterns.GetTasksAsync(ct);
    return await Proxy(resp, ct);
})
    .WithSummary("Available tasks");

api.MapGet("/progress/user_progress", async (HttpContext ctx, ProgressApi pr, CancellationToken ct) =>
{
    if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
        return Results.Unauthorized();
    using var resp = await pr.GetUserProgressAsync(userId, ct);
    return await Proxy(resp, ct);
})
    .RequireAuthorization()
    .WithSummary("Requesting user progress");

api.MapGet("/users/me", async (UsersApi users, CancellationToken ct) =>
{
    using var resp = await users.GetMeAsync(ct);
    return await Proxy(resp, ct);
})
    .RequireAuthorization()
    .WithSummary("Current user profile");



api.MapGet("/users/{id:guid}", async (Guid id, UsersApi users, CancellationToken ct) =>
{
    using var resp = await users.GetUserAsync(id, ct);
    return await Proxy(resp, ct);
})
    .RequireAuthorization()
    .WithSummary("Get user profile");



api.MapPost("/orc/check", async (
    [FromQuery] long taskId,
    IFormFile file,
    HttpContext ctx,
    ProgressApi pr,
    OrchApi orc,
    GatewayObjectStorageClient minioHandler,
    CancellationToken ct) =>
{
    if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
        return Results.Unauthorized();
    Guid checkingId = new();
    await using var stream = file.OpenReadStream();
    await minioHandler.WriteFile(stream, file.FileName, userId, taskId, "load", ct);

    using var resp = await orc.StartCheckAsync(new StartChecking(checkingId, userId, taskId), ct);

    var ans = await Proxy(resp, ct);
    return ans;
})
    .DisableAntiforgery()
    .RequireAuthorization()
    .WithSummary("Code checking");

api.MapPost("/auth/register", async ([FromBody] RegisterRequest req, AuthApi auth, CancellationToken ct) =>
{
    using var resp = await auth.RegisterAsync(req, ct);
    return await Proxy(resp, ct);
})
    .AllowAnonymous()
.WithSummary("Registering a new user");

api.MapPost("/auth/login", async ([FromBody] LoginRequest req, AuthApi auth, CancellationToken ct) =>
{
    using var resp = await auth.LoginAsync(req, ct);
    return await Proxy(resp, ct);
})
    .AllowAnonymous()
    .WithSummary("Authorization");

app.MapHealthChecks("/health/ready");
var webRoot = app.Environment.WebRootPath
              ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uiRoot = Path.Combine(webRoot, "ui");
Directory.CreateDirectory(uiRoot);

app.UseFileServer(new FileServerOptions
{
    RequestPath = "/ui",
    FileProvider = new PhysicalFileProvider(uiRoot),
    EnableDefaultFiles = true
});
app.Run();

static async Task<IResult> Proxy(HttpResponseMessage resp, CancellationToken ct)
{
    var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    var body = await resp.Content.ReadAsStringAsync(ct);
    return Results.Content(body, contentType, Encoding.UTF8, (int)resp.StatusCode);
}





