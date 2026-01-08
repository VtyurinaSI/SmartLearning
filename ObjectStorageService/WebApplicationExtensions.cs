using Microsoft.AspNetCore.Mvc;
using Minio;

namespace ObjectStorageService
{
    public static class WebApplicationExtensions
    {
        public static async Task EnsureObjectStorageAsync(this WebApplication app)
        {
            var bootstrapper = app.Services.GetRequiredService<IStorageBootstrapper>();
            await bootstrapper.EnsureAsync();
        }

        public static void MapObjectStorageEndpoints(this WebApplication app)
        {
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
                if (!CheckStageParser.TryParse(stage, out var s))
                    return Results.BadRequest("stage must be: load|build|reflect|llm");
                var contentType = string.IsNullOrWhiteSpace(req.ContentType)
                    ? "application/octet-stream"
                    : req.ContentType!;

                await using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms, ct);
                var bytes = ms.ToArray();

                if (bytes.Length == 0)
                    return Results.BadRequest("Тело запроса пустое - нечего сохранять");

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

                if (!CheckStageParser.TryParse(stage, out var s))
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

            app.MapGet("/patterns/file", async (
                [FromQuery] string key,
                IMinioClient minio,
                StorageOptions o,
                ILogger<Program> log,
                CancellationToken ct) =>
            {
                try
                {
                    var bytes = await MinioIo.GetAsync(minio, o.PatternsBucket, key, ct);
                    return bytes is null
                        ? Results.NotFound()
                        : Results.File(bytes, "application/octet-stream", Path.GetFileName(key));
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error reading pattern file");
                    return Results.NotFound();
                }
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();
        }
    }
}
