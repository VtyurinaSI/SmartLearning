using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ProgressService
{
    public class UserProgressRepository : IUserProgressRepository
    {
        private readonly NpgsqlDataSource _ds;
        private readonly ILogger<UserProgressRepository> _log;

        public UserProgressRepository(NpgsqlDataSource ds, ILogger<UserProgressRepository> log)
        {
            _ds = ds;
            _log = log;
            _log.LogInformation("ObjectStorageRepository создан");
        }

        public async Task<Guid?> GetUserIdAsync(string userLogin, CancellationToken ct)
        {
            const string sql = """
            select user_id
            from public.users
            where user_login = @UserLogin
            """;
            await using var conn = await _ds.OpenConnectionAsync(ct);
            var userId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(sql, new { UserLogin = userLogin }, cancellationToken: ct));
            if (userId is null) return null;
            return userId;
        }
        public record ProgressRow(long TaskId, bool Compile, bool Test, bool Review);
        public async Task<IReadOnlyList<ProgressRow>> GetUserProgressAsync(Guid userId, CancellationToken ct)
        {
            const string sql = """
                select task_id as TaskId, compile, test, review
                from public.progress
                where user_id = @UserId
                order by task_id
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            var rows = await conn.QueryAsync<ProgressRow>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
            return rows.AsList();
        }
        public async Task SaveCheckingAsync(Guid userId, long taskId, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, CancellationToken ct)
        {
            const string sql = """
                INSERT INTO public.progress AS p (user_id, task_id, task_name, compile, test, review)
                VALUES (@UserId, @TaskId, @TaskName, @Compile, @Test, @Review)
                ON CONFLICT (user_id, task_id) DO UPDATE
                SET compile = EXCLUDED.compile,
                    test    = EXCLUDED.test,
                    review  = EXCLUDED.review
                RETURNING p.task_id;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.QuerySingleOrDefaultAsync<string?>(
               new CommandDefinition(
                   sql,
                   new
                   {
                       UserId = userId,
                       TaskId = taskId,
                       TaskName = "task " + taskId.ToString(),
                       Compile = isCompiledSuccess,
                       Test = isTestedSuccess,
                       Review = isReviewedSuccess
                   },
                   cancellationToken: ct));

        }
    }
}
