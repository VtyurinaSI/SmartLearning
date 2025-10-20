using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ObjectStorageService;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
var opts = builder.Configuration.GetSection("Storage").Get<StorageOptions>()!;

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MinIO client
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

// Ensure bucket exists
await EnsureBucketAsync(mc, opts.Bucket);

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/objects/text", async (
    [FromQuery] string file,
    [FromBody] string content,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var objName = $"{file}.txt";
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    await minio.PutObjectAsync(new PutObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithStreamData(ms)
        .WithObjectSize(ms.Length)
        .WithContentType("text/plain"), ct);

    return Results.Ok(new { bucket = o.Bucket, objectName = objName });
})
.WithName("UploadArbitraryText")
.WithOpenApi();

app.MapPost("/objects/orig-code", async (
    [FromQuery] Guid checkingId,
    [FromQuery] Guid userId, // сейчас не используем
    [FromBody] string origCode,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var objName = $"orig/{checkingId}.txt";
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(origCode));
    await minio.PutObjectAsync(new PutObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithStreamData(ms)
        .WithObjectSize(ms.Length)
        .WithContentType("text/plain"), ct);

    return Results.Ok(new { checkingId });
})
.WithName("SaveOrigCode")
.WithOpenApi();

app.MapPost("/objects/review", async (
    [FromQuery] Guid checkingId,
    [FromBody] string review,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var objName = $"review/{checkingId}.txt";
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(review));
    await minio.PutObjectAsync(new PutObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithStreamData(ms)
        .WithObjectSize(ms.Length)
        .WithContentType("text/plain"), ct);

    return Results.Ok(new { checkingId });
})
.WithName("SaveReview")
.WithOpenApi();

app.MapGet("/objects/orig-code", async (
    [FromQuery] Guid checkingId,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var objName = $"orig/{checkingId}.txt";
    string? text = null;

    await minio.GetObjectAsync(new GetObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithCallbackStream(s =>
        {
            using var sr = new StreamReader(s);
            text = sr.ReadToEnd();
        }), ct);

    return text is null ? Results.NotFound() : Results.Ok(text);
})
.WithName("ReadOrigCode")
.WithOpenApi();

app.MapGet("/objects/review", async (
    [FromQuery] Guid checkingId,
    IMinioClient minio,
    StorageOptions o,
    CancellationToken ct) =>
{
    var objName = $"review/{checkingId}.txt";
    string? text = null;

    await minio.GetObjectAsync(new GetObjectArgs()
        .WithBucket(o.Bucket)
        .WithObject(objName)
        .WithCallbackStream(s =>
        {
            using var sr = new StreamReader(s);
            text = sr.ReadToEnd();
        }), ct);

    return text is null ? Results.NotFound() : Results.Ok(text);
})
.WithName("ReadReview")
.WithOpenApi();

app.Run();

static async Task EnsureBucketAsync(IMinioClient mc, string bucket)
{
    var exists = await mc.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
    if (!exists)
        await mc.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
}
