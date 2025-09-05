using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SmartLearning.Contracts;
using System.Text;
using MinIoStub;

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

builder.Services.AddHttpClient<PatternsApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Patterns"] ?? "http://localhost:6002/"))
    .AddHeaderPropagation();

builder.Services.AddHttpClient<LlmApi>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Llm"] ?? "http://localhost:6003/"))
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

/*api.MapGet("/patterns/{msg}", async ([FromRoute] string msg, PatternsApi patterns, CancellationToken ct) =>
{
    using var resp = await patterns.PingAsync(msg, ct);
    return await Proxy(resp, ct);
})
.WithSummary("Отправка команды в PatternService // заглушка");
*/

api.MapPost("/llm/chat", async ([FromBody] string content, LlmApi llm, CancellationToken ct) =>
{
    using var resp = await llm.ChatAsync(content, ct);
    return await Proxy(resp, ct);
}).WithSummary("Запрос ИИ-ассистенту (LlmService)");

/*api.MapPost("/workflows/llm/chat", async ([FromBody] string content, OrchApi orc, CancellationToken ct) =>
{
    using var resp = await orc.ChatAsync(content, ct);
    return await Proxy(resp, ct);
}).WithSummary("Запрос ИИ-ассистенту через оркестратор");*/

//api.MapPost("/orc/mq", async (StartMqDto content, OrchApi orc, CancellationToken ct) =>
//{
//    using var resp = await orc.ChatAsyncMq(content, ct);
//    return await Proxy(resp, ct);
//}).WithSummary("Запрос ИИ-ассистенту через оркестратор и шину");
api.MapPost("/orc/mq/{msg}", async ([FromRoute] string msg, IObjectStorageRepository repo, OrchApi orc, CancellationToken ct) =>
{

    if (string.IsNullOrWhiteSpace(msg)) return Results.BadRequest("origCode is required");
    Guid checkingId = await repo.SaveOrigCodeAsync(msg, ct);
    using var resp = await orc.ChatAsyncMq(new StartMqDto(true, true, checkingId), ct);
    return await Proxy(resp, ct);
}).WithSummary("Запрос ИИ-ассистенту через оркестратор и шину");

app.MapHealthChecks("/health/ready");

app.Run("http://localhost:5000/");
static async Task<IResult> Proxy(HttpResponseMessage resp, CancellationToken ct)
{
    var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    var body = await resp.Content.ReadAsStringAsync(ct);
    Log.Information("LLM Reviewer Response: {StatusCode} {Body}", resp.StatusCode, body);
    return Results.Content(body, contentType, Encoding.UTF8, (int)resp.StatusCode);
}
public record ChatMessage(string role, string content);
