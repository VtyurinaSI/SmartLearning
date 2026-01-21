namespace ProgressService
{
    public static class WebApplicationExtensions
    {
        public static void UseProgressServiceSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        public static void MapProgressServiceEndpoints(this WebApplication app)
        {
            app.MapGet("/userid/{userLogin}", async (string userLogin, IUserProgressRepository repo, CancellationToken ct) =>
            {
                var userId = await repo.GetUserIdAsync(userLogin, ct);
                return userId is null ? Results.NotFound() : Results.Text(userId.ToString());
            });

            app.MapGet("/user_progress/{userId}", async (Guid userId, IUserProgressRepository repo, CancellationToken ct) =>
            {
                var story = await repo.GetUserProgressAsync(userId, ct);

                CompletedTasks[] compl = story
                    .Where(r => r.CheckResult == true)
                    .Select(r => new CompletedTasks(r.TaskId, string.IsNullOrWhiteSpace(r.TaskName) ? $"task {r.TaskId}" : r.TaskName))
                    .OrderBy(c => c.TaskId)
                    .ToArray();

                InProcessTasks[] inp = story
                    .Where(r => r.CheckResult != true)
                    .Select(r =>
                    {
                        var taskName = string.IsNullOrWhiteSpace(r.TaskName) ? $"task {r.TaskId}" : r.TaskName;
                        if (!r.CompileStat)
                        {
                            return new InProcessTasks(r.TaskId, taskName, CheckingStage.Compilation.ToString(), r.CompileMsg ?? string.Empty, r.IsCheckingFinished ?? false);
                        }
                        if (!r.TestStat)
                        {
                            return new InProcessTasks(r.TaskId, taskName, CheckingStage.Testing.ToString(), r.TestMsg ?? string.Empty, r.IsCheckingFinished ?? false);
                        }
                        return new InProcessTasks(r.TaskId, taskName, CheckingStage.Review.ToString(), r.ReviewMsg ?? string.Empty, r.IsCheckingFinished ?? false);
                    })
                    .ToArray();

                UserProgress prog = new(compl, inp);
                return Results.Json(prog);
            });

            app.MapGet("/user_progress/task/{taskId}", async (long taskId, HttpContext ctx, IUserProgressRepository repo, CancellationToken ct) =>
            {
                if (!Guid.TryParse(ctx.Request.Headers["X-User-Id"], out var headerUserId))
                    return Results.Unauthorized();

                var row = await repo.GetTaskProgressAsync(headerUserId, taskId, ct);
                if (row is null)
                    return Results.NotFound();

                var taskName = string.IsNullOrWhiteSpace(row.TaskName) ? $"task {row.TaskId}" : row.TaskName;
                var result = new TaskProgress(
                    row.TaskId,
                    taskName,
                    row.IsCheckingFinished ?? false,
                    row.CheckResult ?? false,
                    row.CompileStat,
                    row.CompileMsg,
                    row.TestStat,
                    row.TestMsg,
                    row.ReviewStat,
                    row.ReviewMsg);

                return Results.Json(result);
            });
        }
    }
}

