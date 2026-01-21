namespace PatternService;

public static class WebApplicationExtensions
{
    public static void UsePatternServiceSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    public static void MapPatternServiceEndpoints(this WebApplication app)
    {
        app.MapGet("/meta", async (long taskId, TaskCatalogService catalog, CancellationToken ct) =>
        {
            var meta = await catalog.GetMetaAsync(taskId, ct);
            return meta is null ? Results.NotFound() : Results.Json(meta);
        });

        app.MapGet("/task_title", async (long taskId, TaskCatalogService catalog, CancellationToken ct) =>
        {
            var meta = await catalog.GetMetaAsync(taskId, ct);
            return meta is null ? Results.NotFound() : Results.Text(meta.TaskTitle);
        });

        app.MapGet("/tasks", async (TaskCatalogService catalog, CancellationToken ct) =>
        {
            var items = await catalog.GetTaskListAsync(ct);
            return Results.Json(items);
        });

        app.MapGet("/theory", async (long taskId, TaskCatalogService catalog, CancellationToken ct) =>
        {
            var result = await catalog.GetTheoryAsync(taskId, ct);
            return result is null ? Results.NotFound() : Results.File(result.Bytes, result.ContentType, result.FileName);
        });

        app.MapGet("/task", async (long taskId, TaskCatalogService catalog, CancellationToken ct) =>
        {
            var result = await catalog.GetTaskAsync(taskId, ct);
            return result is null ? Results.NotFound() : Results.File(result.Bytes, result.ContentType, result.FileName);
        });

        app.MapGet("/manifest", async (long taskId, TaskCatalogService catalog, CancellationToken ct) =>
        {
            var result = await catalog.GetManifestAsync(taskId, ct);
            return result is null ? Results.NotFound() : Results.File(result.Bytes, result.ContentType, result.FileName);
        });
    }
}
