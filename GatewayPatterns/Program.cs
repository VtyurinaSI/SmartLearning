using GatewayPatterns;
using GatewayPatterns.SrvApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinIoStub;
using Npgsql;
using ObjectStorageService;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SmartLearning.Contracts;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
var cs = builder.Configuration.GetConnectionString("ObjectStorage");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));
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
            Description = "Вставь сюда ТОЛЬКО JWT (без 'Bearer ' — Swagger добавит сам).",
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
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Users"]))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<ProgressApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Progress"]))
    .AddHeaderPropagation();

builder.Services.AddHttpClient<OrchApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Orch"]))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<AuthApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Auth"]))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<IObjectStorageClient, ObjectStorageClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!); // "http://object-storage-service:8080/"
});

builder.Services.AddTransient<GatewayObjectStorageClient>();
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Sixteen,
        outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"));
builder.Services.AddObjectStorage(builder.Configuration);
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
api.MapPost("/file", ([FromForm] IFormFile file) =>
{

    return Results.Ok($"Получили {file.FileName}!");
})
    .DisableAntiforgery()
   .Accepts<IFormFile>("multipart/form-data")
   .Produces(StatusCodes.Status200OK)
   .WithOpenApi();

api.MapGet("/ping", () =>
{
    return "pong";
})
.WithSummary("Отправка команды в UserService // заглушка");

api.MapGet("/users/{msg}", async ([FromRoute] string msg, UsersApi users, CancellationToken ct) =>
{
    using var resp = await users.PingAsync(msg, ct);
    return await Proxy(resp, ct);
})
.WithSummary("Отправка команды в UserService // заглушка");

api.MapGet("/progress/user_progress", async (HttpContext ctx, ProgressApi pr, CancellationToken ct) =>
{
    if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
        return Results.Unauthorized();
    using var resp = await pr.GetUserProgressAsync(userId, ct);
    return await Proxy(resp, ct);
})
    .RequireAuthorization()
.WithSummary("Запрос прогресса пользователя");

api.MapPost("/orc/check", async ([FromBody] RecievedForChecking msg, HttpContext ctx, ProgressApi pr, IObjectStorageRepository repo, OrchApi orc, CancellationToken ct,
    IObjectStorageClient minio, GatewayObjectStorageClient minioHandler) =>
{
    if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
        return Results.Unauthorized();
    Guid checkingId = await repo.SaveOrigCodeAsync(msg.OrigCode, userId, ct);
    await minioHandler.WriteFile(msg.OrigCode, checkingId, userId, msg.TaskId, "load", ct);

    using var resp = await orc.StartCheckAsync(new StartChecking(checkingId, userId, msg.TaskId), ct);

    var ans = await Proxy(resp, ct);
    return ans;
})
    .RequireAuthorization()
.WithSummary("Проверка кода");

api.MapPost("/auth/register", async ([FromBody] RegisterRequest req, AuthApi auth, CancellationToken ct) =>
{
    using var resp = await auth.RegisterAsync(req, ct);
    return await Proxy(resp, ct);
})
    .AllowAnonymous()
.WithSummary("Регистрация нового пользователя");

api.MapPost("/auth/login", async ([FromBody] LoginRequest req, AuthApi auth, CancellationToken ct) =>
{
    using var resp = await auth.LoginAsync(req, ct);
    return await Proxy(resp, ct);
})
    .AllowAnonymous()
    .WithSummary("Авторизация");

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

