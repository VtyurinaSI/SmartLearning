using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ObjectStorageService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
var opts = builder.Configuration.GetSection("Storage").Get<StorageOptions>()!;

// MinIO клиент
var minio = new MinioClient()
    .WithEndpoint(new Uri(opts.Endpoint).Host, new Uri(opts.Endpoint).Port)
    .WithCredentials(opts.AccessKey, opts.SecretKey)
    .WithSSL(opts.Endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase))
    .Build();
builder.Services.AddSingleton(minio);
builder.Services.AddSingleton(opts);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/objects/orig-code", async ([FromQuery] Guid checkingId, [FromQuery] Guid userId, [FromBody] string origCode,
                                         IMinioClient mc, StorageOptions o, CancellationToken ct) =>
{
    var objName = $"orig/{checkingId}.txt";
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(origCode));
    await mc.PutObjectAsync(new PutObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithStreamData(ms)
        .WithObjectSize(ms.Length)
        .WithContentType("text/plain; charset=utf-8"), ct);
    return Results.Ok(new { checkingId, userId, objectName = objName });
});

app.MapPost("/objects/review", async ([FromQuery] Guid checkingId, [FromBody] string review,
                                      IMinioClient mc, StorageOptions o, CancellationToken ct) =>
{
    var objName = $"review/{checkingId}.txt";
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(review));
    await mc.PutObjectAsync(new PutObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithStreamData(ms)
        .WithObjectSize(ms.Length)
        .WithContentType("text/plain; charset=utf-8"), ct);
    return Results.Ok(new { checkingId, objectName = objName });
});

app.MapGet("/objects/orig-code/{checkingId:guid}", async (Guid checkingId, IMinioClient mc, StorageOptions o, CancellationToken ct) =>
{
    var objName = $"orig/{checkingId}.txt";
    string? text = null;
    await mc.GetObjectAsync(new GetObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithCallbackStream(s =>
        {
            using var sr = new StreamReader(s);
            text = sr.ReadToEnd();
        }), ct);
    return text is null ? Results.NotFound() : Results.Ok(text);
});

app.MapGet("/objects/review/{checkingId:guid}", async (Guid checkingId, IMinioClient mc, StorageOptions o, CancellationToken ct) =>
{
    var objName = $"review/{checkingId}.txt";
    string? text = null;
    await mc.GetObjectAsync(new GetObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithCallbackStream(s =>
        {
            using var sr = new StreamReader(s);
            text = sr.ReadToEnd();
        }), ct);
    return text is null ? Results.NotFound() : Results.Ok(text);
});

app.Run();