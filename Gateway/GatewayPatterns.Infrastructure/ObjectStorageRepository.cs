using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GatewayPatterns.Infrastructure
{
    public class ObjectStorageRepository : IObjectStorageRepository
    {
        private readonly NpgsqlDataSource _ds;
        private readonly ILogger<ObjectStorageRepository> _log;

        public ObjectStorageRepository(NpgsqlDataSource ds, ILogger<ObjectStorageRepository> log)
        {
            _ds = ds;
            _log = log;
            _log.LogInformation("ObjectStorageRepository создан");
        }

        public async Task<Guid> SaveSourceAsync(string origCode, CancellationToken ct)
        {
            var checkingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            const string sql = """
            insert into public.objectstorage (checking_id, user_id, orig_code)
            values (@CheckingId, @UserId, @OrigCode);
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);

            var args = new { CheckingId = checkingId, UserId = userId, OrigCode = origCode };
            await conn.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));

            _log.LogInformation("Saved source. checking_id={CheckingId} user_id={UserId}", checkingId, userId);
            return checkingId;
        }
        public async Task<string?> ReadReviewAsync(Guid checkingId, CancellationToken ct)
        {
            const string sql = """
            select review_res
            from public.objectstorage
            where checking_id = @CheckingId
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            return await conn.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(sql, new { CheckingId = checkingId }, cancellationToken: ct));

        }
    }
}
