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

        public async Task SaveCheckingAsync(Guid userId, long taskId, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, CancellationToken ct)
        {
            const string sql = """
            update public.objectstorage
            set compil_res = @CompilRes
            where checking_id = @CheckingId
            """;

            //await using var conn = await _ds.OpenConnectionAsync(ct);
            //await conn.QuerySingleOrDefaultAsync<string?>(
            //   new CommandDefinition(sql, new { CheckingId = checkingId, CompilRes = compileRes }, cancellationToken: ct));


        }
    }
}
