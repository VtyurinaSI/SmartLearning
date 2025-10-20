using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using System.Text;

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

var app = builder.Build();

await EnsureBucketAsync(mc, opts.Bucket);

// swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/objects/{stage}/file", async (
    [FromRoute] string stage,
    [FromQuery] Guid userId,
    [FromQuery] long taskId,
    [FromQuery] string? name,
    [FromBody] string body,          
    HttpRequest req,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    if (!TryParseStage(stage, out var s)) return Results.BadRequest("stage must be: build|reflect|llm");

    var fileName = string.IsNullOrWhiteSpace(name)
        ? $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt"
        : name;

    var contentType = string.IsNullOrWhiteSpace(req.ContentType) ? "text/plain" : req.ContentType;
    var bytes = Encoding.UTF8.GetBytes(body);

    var key = StorageKeys.File(userId, taskId, s, fileName);
    await MinioIo.PutAsync(minio, o.Bucket, key, bytes, contentType, ct);

    return Results.Ok(new { key, bucket = o.Bucket, size = bytes.LongLength, contentType });
})
.Accepts<string>("text/plain")
.Accepts<string>("application/json")
.Produces(StatusCodes.Status200OK)
.WithOpenApi();

app.MapGet("/objects/{stage}/file", async (
    [FromRoute] string stage,
    [FromQuery] Guid userId,
    [FromQuery] long taskId,
    [FromQuery] string name,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    if (!TryParseStage(stage, out var s)) return Results.BadRequest("stage must be: build|reflect|llm");
    if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("name is required");

    var key = StorageKeys.File(userId, taskId, s, name);
    var bytes = await MinioIo.GetAsync(minio, o.Bucket, key, ct);
    return bytes is null
        ? Results.NotFound()
        : Results.File(bytes, "application/octet-stream", fileDownloadName: name);
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapGet("/objects/{stage}/list", async (
    [FromRoute] string stage,
    [FromQuery] Guid userId,
    [FromQuery] long taskId,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    if (!TryParseStage(stage, out var s)) return Results.BadRequest("stage must be: build|reflect|llm");

    var prefix = StorageKeys.StagePrefix(userId, taskId, s) + "/";
    var list = await MinioIo.ListKeysAsync(minio, o.Bucket, prefix, recursive: true, ct);
    return Results.Ok(list);
})
.Produces<string[]>(StatusCodes.Status200OK)
.WithOpenApi();

app.MapPost("/objects/text", async (
    [FromQuery] string file,
    [FromBody] string content,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var key = $"misc/{file}.txt";
    await MinioIo.PutAsync(minio, o.Bucket, key, Encoding.UTF8.GetBytes(content), "text/plain", ct);
    return Results.Ok(new { key, bucket = o.Bucket });
})
.WithOpenApi();

app.Run();



static bool TryParseStage(string stage, out CheckStage s)
{
    switch (stage.ToLowerInvariant())
    {
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



enum CheckStage { Build, Reflect, Llm }

static class StorageKeys
{
    public static string StageSegment(CheckStage stage) => stage switch
    {
        CheckStage.Build => "01-build",
        CheckStage.Reflect => "02-reflect",
        CheckStage.Llm => "03-llm",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };

    public static string Base(Guid userId, long taskId)
        => $"submissions/{userId:N}/{taskId}";

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
            .WithBucket(bucket).WithObject(key)
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
