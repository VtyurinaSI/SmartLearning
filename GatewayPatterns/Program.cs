using GatewayPatterns.SrvApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinIoStub;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SmartLearning.Contracts;
using System.Net;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Users"] ?? "http://localhost:6001/"))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<ProgressApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Progress"] ?? "http://localhost:6010/"))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<PatternsApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Patterns"] ?? "http://localhost:6002/"))
    .AddHeaderPropagation();

builder.Services.AddHttpClient<OrchApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Orch"] ?? "http://localhost:6000/"))
    .AddHeaderPropagation();
builder.Services.AddHttpClient<AuthApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Auth"] ?? "http://localhost:5164/"))
    .AddHeaderPropagation();

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
    .AddUrlGroup(new Uri($"{builder.Configuration["Downstream:Patterns"]}health/ready"), name: "patterns_svc")
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
            ClockSkew = TimeSpan.FromMinutes(10)
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

api.MapPost("/orc/check", async ([FromBody] RecievedForChecking msg, HttpContext ctx, ProgressApi pr, IObjectStorageRepository repo, OrchApi orc, CancellationToken ct) =>
{
    if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var userId))
        return Results.Unauthorized();
    Guid checkingId = await repo.SaveOrigCodeAsync(msg.OrigCode, userId, ct);

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
}).AllowAnonymous();

api.MapPost("/auth/login", async ([FromBody] LoginRequest req, AuthApi auth, CancellationToken ct) =>
{
    using var resp = await auth.LoginAsync(req, ct);
    return await Proxy(resp, ct);
}).AllowAnonymous();

app.MapHealthChecks("/health/ready");

app.Run("http://localhost:5000/");

static async Task<IResult> Proxy(HttpResponseMessage resp, CancellationToken ct)
{
    var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    var body = await resp.Content.ReadAsStringAsync(ct);
    return Results.Content(body, contentType, Encoding.UTF8, (int)resp.StatusCode);
}

/*static async Task<Guid?> GetUserIdByLoginAsync(string login, ProgressApi pr, CancellationToken ct)
{
    using var userIdResp = await pr.GetUserIdAsync(login, ct);

    if (userIdResp.StatusCode == HttpStatusCode.NotFound)
        return null;

    userIdResp.EnsureSuccessStatusCode();

    Guid userId;
    var mediaType = userIdResp.Content.Headers.ContentType?.MediaType;

    if (string.Equals(mediaType, "text/plain", StringComparison.OrdinalIgnoreCase))
    {
        var s = await userIdResp.Content.ReadAsStringAsync(ct);
        userId = Guid.Parse(s.Trim('"', ' ', '\n', '\r', '\t'));
    }
    else
    {
        var uid = await userIdResp.Content.ReadFromJsonAsync<Guid?>(cancellationToken: ct);
        if (uid is null) return null;
        userId = uid.Value;
    }
    return userId;

}*/