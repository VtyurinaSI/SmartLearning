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

                ComplitedTasks[] compl = story
                    .Where(r => r.CheckResult == true)
                    .Select(r => new ComplitedTasks(r.TaskId, string.IsNullOrWhiteSpace(r.TaskName) ? $"task {r.TaskId}" : r.TaskName))
                    .OrderBy(c => c.TaskId)
                    .ToArray();

                InProcessTasks[] inp = story
                    .Where(r => r.CheckResult != true)
                    .Select(r =>
                    {
                        var taskName = string.IsNullOrWhiteSpace(r.TaskName) ? $"task {r.TaskId}" : r.TaskName;
                        if (!r.CompileStat)
                        {
                            return new InProcessTasks(r.TaskId, taskName, CheckingStage.Compilation.ToString(), r.CompileMsg ?? string.Empty);
                        }
                        if (!r.TestStat)
                        {
                            return new InProcessTasks(r.TaskId, taskName, CheckingStage.Testing.ToString(), r.TestMsg ?? string.Empty);
                        }
                        return new InProcessTasks(r.TaskId, taskName, CheckingStage.Review.ToString(), r.ReviewMsg ?? string.Empty);
                    })
                    .ToArray();

                UserProgress prog = new(compl, inp);
                return Results.Json(prog);
            });
        }
    }
}
