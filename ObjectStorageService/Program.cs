using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using System;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
var opts = builder.Configuration.GetSection("Storage").Get<StorageOptions>()!;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var uri = new Uri(opts.Endpoint);
var mcBuilder = new MinioClient()
    .WithEndpoint(uri.Host, uri.Port)
    .WithCredentials(opts.AccessKey, opts.SecretKey);
if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
    mcBuilder = mcBuilder.WithSSL();

var mc = mcBuilder.Build();
builder.Services.AddSingleton<IMinioClient>(mc);
builder.Services.AddSingleton(opts);
var factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug)
    .AddSimpleConsole(opt =>
    {
        opt.TimestampFormat = "HH:mm:ss.fff ";
        opt.UseUtcTimestamp = true;
        opt.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
        opt.SingleLine = true;
    }));

ILogger<Program> log = factory.CreateLogger<Program>();
builder.Services.AddSingleton(log);

var app = builder.Build();

await EnsureBucketAsync(mc, opts.Bucket);

app.UseSwagger();
app.UseSwaggerUI();
app.MapPost("/objects/{stage}/file", async (
    HttpRequest req,
    [FromRoute] string stage,
    [FromQuery] Guid userId,
    [FromQuery] long taskId,
    [FromQuery] string fileName,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    if (!TryParseStage(stage, out var s))
        return Results.BadRequest("stage must be: load|build|reflect|llm");
    var contentType = string.IsNullOrWhiteSpace(req.ContentType)
        ? "application/octet-stream"
        : req.ContentType!;

    await using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms, ct);
    var bytes = ms.ToArray();

    if (bytes.Length == 0)
        return Results.BadRequest("Тело запроса пустое – нечего сохранять");

    var key = StorageKeys.File(userId, taskId, s, fileName);
    if (s == CheckStage.Load)
        await MinioIo.ClearSubmissionAsync(minio, o.Bucket, userId, taskId, ct);
    await MinioIo.PutAsync(minio, o.Bucket, key, bytes, contentType, ct);

    return Results.Ok(new { key, bucket = o.Bucket, size = bytes.LongLength, contentType });
});


app.MapGet("/objects/{stage}/file", async (
    [FromRoute] string stage,
    [FromQuery] Guid userId,
    [FromQuery] long taskId,
    [FromQuery] string? fileName,
    IMinioClient minio,
    StorageOptions o,
    ILogger<Program> log,
    CancellationToken ct) =>
{
    log.LogInformation("Enter in /objects/{stage}/file", stage);

    if (!TryParseStage(stage, out var s))
        return Results.BadRequest("stage must be: load|build|reflect|llm");

    if (!string.IsNullOrWhiteSpace(fileName))
    {
        var key = StorageKeys.File(userId, taskId, s, fileName);

        try
        {
            var bytes = await MinioIo.GetAsync(minio, o.Bucket, key, ct);
            return bytes is null
                ? Results.NotFound()
                : Results.File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error reading file");
            return Results.NotFound();
        }
    }

    var prefix = StorageKeys.StagePrefix(userId, taskId, s) + "/";
    var keys = await MinioIo.ListKeysAsync(minio, o.Bucket, prefix, recursive: true, ct);

    if (keys.Count == 0)
        return Results.NotFound("Файлов нет");

    if (keys.Count == 1)
    {
        var key = keys[0];
        var bytes = await MinioIo.GetAsync(minio, o.Bucket, key, ct);

        if (bytes is null)
            return Results.NotFound();

        var name = Path.GetFileName(key);
        return Results.File(bytes, "application/octet-stream", name);
    }

    using var zipMs = new MemoryStream();
    using (var zip = new System.IO.Compression.ZipArchive(
        zipMs,
        System.IO.Compression.ZipArchiveMode.Create,
        leaveOpen: true))
    {
        foreach (var key in keys)
        {
            var bytes = await MinioIo.GetAsync(minio, o.Bucket, key, ct);
            if (bytes is null) continue;

            var entryName = key.Substring(prefix.Length);
            var entry = zip.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Fastest);

            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(bytes, ct);
        }
    }

    zipMs.Position = 0;
    return Results.File(
        zipMs.ToArray(),
        "application/zip",
        $"{stage}-{taskId}.zip");
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();


app.Run();

static bool TryParseStage(string stage, out CheckStage s)
{
    switch (stage.ToLowerInvariant())
    {
        case "load": s = CheckStage.Load; return true;
        case "build": s = CheckStage.Build; return true;
        case "reflect": s = CheckStage.Reflect; return true;
        case "llm": s = CheckStage.Llm; return true;
        default: s = default; return false;
    }
}

static async Task EnsureBucketAsync(IMinioClient mc, string bucket)
{
    var exists = await mc.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
    if (!exists) await mc.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
}

enum CheckStage { Load, Build, Reflect, Llm }

static class StorageKeys
{
    public static string StageSegment(CheckStage stage) => stage switch
    {
        CheckStage.Load => "00-load",
        CheckStage.Build => "01-build",
        CheckStage.Reflect => "02-reflect",
        CheckStage.Llm => "03-llm",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };

    public static string Base(Guid userId, long taskId)
        => $"submissions/{userId}/{taskId}";

    public static string StagePrefix(Guid userId, long taskId, CheckStage stage)
        => $"{Base(userId, taskId)}/{StageSegment(stage)}";

    public static string File(Guid userId, long taskId, CheckStage stage, string name)
        => $"{StagePrefix(userId, taskId, stage)}/{name}";
}

sealed class StorageOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = "smartlearning";
}

static class MinioIo
{

    public static async Task ClearSubmissionAsync(IMinioClient mc, string bucket, Guid userId, long taskId, CancellationToken ct)
    {
        var prefix = $"submissions/{userId:D}/{taskId}/";

        var objects = mc.ListObjectsEnumAsync(
            new ListObjectsArgs()
                .WithBucket(bucket)
                .WithPrefix(prefix)
                .WithRecursive(true),
            ct);

        await foreach (var obj in objects.WithCancellation(ct))
        {
            await mc.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(obj.Key),
                ct);
        }

    }

    public static async Task PutAsync(IMinioClient mc, string bucket, string key, byte[] bytes, string contentType, CancellationToken ct)
    {


        using var ms = new MemoryStream(bytes);
        await mc.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket).WithObject(key)
            .WithStreamData(ms).WithObjectSize(ms.Length)
            .WithContentType(contentType), ct);
    }

    public static async Task<byte[]?> GetAsync(IMinioClient mc, string bucket, string key, CancellationToken ct)
    {
        byte[]? result = null;

        await mc.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(key)
            .WithCallbackStream(s =>
            {
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                result = ms.ToArray();
            }), ct);
        return result;
    }

    public static Task<List<string>> ListKeysAsync(IMinioClient mc, string bucket, string prefix, bool recursive, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<List<string>>();
        var acc = new List<string>();

        var obs = mc.ListObjectsAsync(new ListObjectsArgs()
            .WithBucket(bucket)
            .WithPrefix(prefix)
            .WithRecursive(recursive));

        var sub = obs.Subscribe(
            onNext: (Item it) =>
            {
                if (!string.IsNullOrEmpty(it.Key) && !it.Key.EndsWith("/"))
                    acc.Add(it.Key);
            },
            onError: ex => tcs.TrySetException(ex),
            onCompleted: () => tcs.TrySetResult(acc)
        );

        ct.Register(() =>
        {
            sub.Dispose();
            tcs.TrySetCanceled(ct);
        });

        return tcs.Task;
    }

}