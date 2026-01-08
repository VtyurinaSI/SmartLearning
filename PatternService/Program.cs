using Dapper;
using Npgsql;
using PatternService;
using System.Data;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cs = builder.Configuration.GetConnectionString("PatternsCatalog");
if (string.IsNullOrWhiteSpace(cs))
    cs = builder.Configuration.GetConnectionString("Default");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));
builder.Services.AddScoped<ITaskCatalogRepository, TaskCatalogRepository>();

builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection("Catalog"));

builder.Services.AddHttpClient<IContentStorage, ContentStorage>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.MapGet("/meta", async (long taskId, ITaskCatalogRepository repo, CancellationToken ct) =>
{
    var meta = await repo.GetMetaAsync(taskId, ct);
    return meta is null ? Results.NotFound() : Results.Json(meta);
});

app.MapGet("/tasks", async (ITaskCatalogRepository repo, IContentStorage storage, Microsoft.Extensions.Options.IOptions<CatalogOptions> opts, CancellationToken ct) =>
{
    var metas = await repo.GetAllMetaAsync(ct);
    var o = opts.Value;

    async Task<TaskListItem> BuildItemAsync(TaskMeta meta)
    {
        var key = BuildKey(o.BasePrefix, meta.TaskId, meta.Version, o.TaskFileName);
        var bytes = await storage.GetAsync(key, ct);
        var text = bytes is null ? string.Empty : Encoding.UTF8.GetString(bytes);
        text = text.TrimStart('\uFEFF');
        var snippet = text.Length > 100 ? text[..(100-3)]+"..." : text;
        return new TaskListItem(meta.TaskId, meta.PatternTitle, snippet);
    }

    var items = await Task.WhenAll(metas.Select(BuildItemAsync));
    return Results.Json(items);
});

app.MapGet("/theory", async (long taskId, ITaskCatalogRepository repo, IContentStorage storage, Microsoft.Extensions.Options.IOptions<CatalogOptions> opts, CancellationToken ct) =>
{
    var meta = await repo.GetMetaAsync(taskId, ct);
    if (meta is null) return Results.NotFound();

    var o = opts.Value;
    var key = BuildKey(o.BasePrefix, taskId, meta.Version, o.TheoryFileName);
    var bytes = await storage.GetAsync(key, ct);
    return bytes is null ? Results.NotFound() : Results.File(bytes, "text/markdown; charset=utf-8", o.TheoryFileName);
});

app.MapGet("/task", async (long taskId, ITaskCatalogRepository repo, IContentStorage storage, Microsoft.Extensions.Options.IOptions<CatalogOptions> opts, CancellationToken ct) =>
{
    var meta = await repo.GetMetaAsync(taskId, ct);
    if (meta is null) return Results.NotFound();

    var o = opts.Value;
    var key = BuildKey(o.BasePrefix, taskId, meta.Version, o.TaskFileName);
    var bytes = await storage.GetAsync(key, ct);
    return bytes is null ? Results.NotFound() : Results.File(bytes, "text/markdown; charset=utf-8", o.TaskFileName);
});

app.MapGet("/manifest", async (long taskId, ITaskCatalogRepository repo, IContentStorage storage, Microsoft.Extensions.Options.IOptions<CatalogOptions> opts, CancellationToken ct) =>
{
    var meta = await repo.GetMetaAsync(taskId, ct);
    if (meta is null) return Results.NotFound();

    var o = opts.Value;
    var key = BuildKey(o.BasePrefix, taskId, meta.Version, o.ManifestFileName);
    var bytes = await storage.GetAsync(key, ct);
    return bytes is null ? Results.NotFound() : Results.File(bytes, "application/json", o.ManifestFileName);
});

app.Run();

static string BuildKey(string basePrefix, long taskId, int version, string fileName)
    => $"{basePrefix}/{taskId}/v{version}/{fileName}";
