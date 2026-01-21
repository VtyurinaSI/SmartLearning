using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace ProgressService
{
    public class UserProgressRepository : IUserProgressRepository
    {
        private const string HasCheckResultSql =
            "select count(*) from information_schema.columns where table_schema = 'public' and table_name = 'progress' and column_name = 'check_result'";
        private const string HasIsCheckingFinishedSql =
            "select count(*) from information_schema.columns where table_schema = 'public' and table_name = 'progress' and column_name = 'is_checking_finished'";

        private readonly NpgsqlDataSource _ds;
        private readonly ILogger<UserProgressRepository> _log;

        public UserProgressRepository(NpgsqlDataSource ds, ILogger<UserProgressRepository> log)
        {
            _ds = ds;
            _log = log;
            _log.LogInformation("UserProgressRepository created");
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

        public record ProgressRow(long TaskId, string? TaskName, bool CompileStat, string? CompileMsg, bool TestStat, string? TestMsg, bool ReviewStat, string? ReviewMsg, bool? IsCheckingFinished, bool? CheckResult, DateTime UpdatedAt);

        public async Task<IReadOnlyList<ProgressRow>> GetUserProgressAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);

            var hasCheckResult = await HasCheckResultColumnAsync(conn, ct);
            var hasIsCheckingFinished = await HasIsCheckingFinishedColumnAsync(conn, ct);

            string sql;
            if (hasCheckResult && hasIsCheckingFinished)
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       is_checking_finished as IsCheckingFinished,
                       check_result as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId
                order by task_id
                """;
            }
            else if (hasCheckResult)
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       check_result as IsCheckingFinished,
                       check_result as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId
                order by task_id
                """;
            }
            else
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       (compile_stat and test_stat and review_stat) as IsCheckingFinished,
                       (compile_stat and test_stat and review_stat) as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId
                order by task_id
                """;
            }

            var rows = await conn.QueryAsync<ProgressRow>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
            return rows.AsList();
        }

        public async Task<ProgressRow?> GetTaskProgressAsync(Guid userId, long taskId, CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);

            var hasCheckResult = await HasCheckResultColumnAsync(conn, ct);
            var hasIsCheckingFinished = await HasIsCheckingFinishedColumnAsync(conn, ct);

            string sql;
            if (hasCheckResult && hasIsCheckingFinished)
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       is_checking_finished as IsCheckingFinished,
                       check_result as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId and task_id = @TaskId
                """;
            }
            else if (hasCheckResult)
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       check_result as IsCheckingFinished,
                       check_result as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId and task_id = @TaskId
                """;
            }
            else
            {
                sql = """
                select task_id as TaskId,
                       task_name as TaskName,
                       compile_stat as CompileStat,
                       compile_msg as CompileMsg,
                       test_stat as TestStat,
                       test_msg as TestMsg,
                       review_stat as ReviewStat,
                       review_msg as ReviewMsg,
                       (compile_stat and test_stat and review_stat) as IsCheckingFinished,
                       (compile_stat and test_stat and review_stat) as CheckResult,
                       updated_at as UpdatedAt
                from public.progress
                where user_id = @UserId and task_id = @TaskId
                """;
            }

            return await conn.QuerySingleOrDefaultAsync<ProgressRow>(
                new CommandDefinition(sql, new { UserId = userId, TaskId = taskId }, cancellationToken: ct));
        }

        public async Task SaveCheckingAsync(Guid userId, long taskId, string taskName, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, Guid? correlationId, bool isCheckingFinished, bool checkResult, string? compileMsg, string? testMsg, string? reviewMsg, CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);

            var hasCheckResult = await HasCheckResultColumnAsync(conn, ct);
            var hasIsCheckingFinished = await HasIsCheckingFinishedColumnAsync(conn, ct);
            var normalizedTaskName = string.IsNullOrWhiteSpace(taskName) ? $"task {taskId}" : taskName;

            string sql;
            if (hasCheckResult && hasIsCheckingFinished)
            {
                sql = """
                INSERT INTO public.progress AS p (user_id, task_id, task_name, correlation_id, is_checking_finished, check_result, compile_stat, compile_msg, test_stat, test_msg, review_stat, review_msg, updated_at)
                VALUES (@UserId, @TaskId, @TaskName, @CorrelationId, @IsCheckingFinished, @CheckResult, @CompileStat, @CompileMsg, @TestStat, @TestMsg, @ReviewStat, @ReviewMsg, now())
                ON CONFLICT (user_id, task_id) DO UPDATE
                SET task_name      = EXCLUDED.task_name,
                    correlation_id = EXCLUDED.correlation_id,
                    is_checking_finished = EXCLUDED.is_checking_finished,
                    check_result   = EXCLUDED.check_result,
                    compile_stat   = EXCLUDED.compile_stat,
                    compile_msg    = EXCLUDED.compile_msg,
                    test_stat      = EXCLUDED.test_stat,
                    test_msg       = EXCLUDED.test_msg,
                    review_stat    = EXCLUDED.review_stat,
                    review_msg     = EXCLUDED.review_msg,
                    updated_at     = now()
                RETURNING p.task_id;
                """;

                await conn.ExecuteAsync(
                   new CommandDefinition(
                       sql,
                       new
                       {
                           UserId = userId,
                           TaskId = taskId,
                           TaskName = normalizedTaskName,
                           CorrelationId = correlationId,
                           IsCheckingFinished = isCheckingFinished,
                           CheckResult = checkResult,
                           CompileStat = isCompiledSuccess,
                           CompileMsg = compileMsg,
                           TestStat = isTestedSuccess,
                           TestMsg = testMsg,
                           ReviewStat = isReviewedSuccess,
                           ReviewMsg = reviewMsg
                       },
                       cancellationToken: ct));
            }
            else if (hasCheckResult)
            {
                sql = """
                INSERT INTO public.progress AS p (user_id, task_id, task_name, correlation_id, check_result, compile_stat, compile_msg, test_stat, test_msg, review_stat, review_msg, updated_at)
                VALUES (@UserId, @TaskId, @TaskName, @CorrelationId, @CheckResult, @CompileStat, @CompileMsg, @TestStat, @TestMsg, @ReviewStat, @ReviewMsg, now())
                ON CONFLICT (user_id, task_id) DO UPDATE
                SET task_name      = EXCLUDED.task_name,
                    correlation_id = EXCLUDED.correlation_id,
                    check_result   = EXCLUDED.check_result,
                    compile_stat   = EXCLUDED.compile_stat,
                    compile_msg    = EXCLUDED.compile_msg,
                    test_stat      = EXCLUDED.test_stat,
                    test_msg       = EXCLUDED.test_msg,
                    review_stat    = EXCLUDED.review_stat,
                    review_msg     = EXCLUDED.review_msg,
                    updated_at     = now()
                RETURNING p.task_id;
                """;

                await conn.ExecuteAsync(
                   new CommandDefinition(
                       sql,
                       new
                       {
                           UserId = userId,
                           TaskId = taskId,
                           TaskName = normalizedTaskName,
                           CorrelationId = correlationId,
                           CheckResult = checkResult,
                           CompileStat = isCompiledSuccess,
                           CompileMsg = compileMsg,
                           TestStat = isTestedSuccess,
                           TestMsg = testMsg,
                           ReviewStat = isReviewedSuccess,
                           ReviewMsg = reviewMsg
                       },
                       cancellationToken: ct));
            }
            else
            {
                sql = """
                INSERT INTO public.progress AS p (user_id, task_id, task_name, correlation_id, compile_stat, compile_msg, test_stat, test_msg, review_stat, review_msg, updated_at)
                VALUES (@UserId, @TaskId, @TaskName, @CorrelationId, @CompileStat, @CompileMsg, @TestStat, @TestMsg, @ReviewStat, @ReviewMsg, now())
                ON CONFLICT (user_id, task_id) DO UPDATE
                SET task_name      = EXCLUDED.task_name,
                    correlation_id = EXCLUDED.correlation_id,
                    compile_stat   = EXCLUDED.compile_stat,
                    compile_msg    = EXCLUDED.compile_msg,
                    test_stat      = EXCLUDED.test_stat,
                    test_msg       = EXCLUDED.test_msg,
                    review_stat    = EXCLUDED.review_stat,
                    review_msg     = EXCLUDED.review_msg,
                    updated_at     = now()
                RETURNING p.task_id;
                """;

                await conn.ExecuteAsync(
                   new CommandDefinition(
                       sql,
                       new
                       {
                           UserId = userId,
                           TaskId = taskId,
                           TaskName = normalizedTaskName,
                           CorrelationId = correlationId,
                           CompileStat = isCompiledSuccess,
                           CompileMsg = compileMsg,
                           TestStat = isTestedSuccess,
                           TestMsg = testMsg,
                           ReviewStat = isReviewedSuccess,
                           ReviewMsg = reviewMsg
                       },
                       cancellationToken: ct));
            }

        }

        private static async Task<bool> HasCheckResultColumnAsync(IDbConnection conn, CancellationToken ct)
        {
            var count = await conn.QuerySingleAsync<int>(
                new CommandDefinition(HasCheckResultSql, cancellationToken: ct));
            return count > 0;
        }

        private static async Task<bool> HasIsCheckingFinishedColumnAsync(IDbConnection conn, CancellationToken ct)
        {
            var count = await conn.QuerySingleAsync<int>(
                new CommandDefinition(HasIsCheckingFinishedSql, cancellationToken: ct));
            return count > 0;
        }
    }
}

