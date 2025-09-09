using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinIoStub;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SmartLearning.Contracts;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHeaderPropagation(o =>
{
    o.Headers.Add("Authorization");
    o.Headers.Add("X-Correlation-Id");
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

var api = app.MapGroup("/api");

api.MapGet("/users/{msg}", async ([FromRoute] string msg, UsersApi users, CancellationToken ct) =>
{
    using var resp = await users.PingAsync(msg, ct);
    return await Proxy(resp, ct);
})
.WithSummary("Отправка команды в UserService // заглушка");


api.MapPost("/orc/check", async ([FromBody] RecievedForChecking msg, ProgressApi pr, IObjectStorageRepository repo, OrchApi orc, CancellationToken ct) =>
{
    using var userIdResp = await pr.PingAsync(msg.UserLogin, ct);

    if (userIdResp.StatusCode == HttpStatusCode.NotFound)
        return Results.NotFound($"Пользователь '{msg.UserLogin}' не найден.");
    
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
        if (uid is null) return Results.Problem("Progress вернул пустой GUID.");
        userId = uid.Value;
    }
    Guid checkingId = await repo.SaveOrigCodeAsync(msg.OrigCode, userId, ct);

    using var resp = await orc.StartCheckAsync(new StartChecking(checkingId, userId, msg.TaskId), ct);
    return await Proxy(resp, ct);
}).WithSummary("Запрос ИИ-ассистенту через оркестратор и шину");

app.MapHealthChecks("/health/ready");

app.Run("http://localhost:5000/");
static async Task<IResult> Proxy(HttpResponseMessage resp, CancellationToken ct)
{
    var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    var body = await resp.Content.ReadAsStringAsync(ct);
    return Results.Content(body, contentType, Encoding.UTF8, (int)resp.StatusCode);
}
