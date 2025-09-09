using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MinIoStub
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

        public async Task<Guid> SaveOrigCodeAsync(string origCode, Guid userId, CancellationToken ct)
        {
            var checkingId = Guid.NewGuid();

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
        public async Task<string?> ReadOrigCodeAsync(Guid checkingId, CancellationToken ct)
        {
            const string sql = """
            select orig_code
            from public.objectstorage
            where checking_id = @CheckingId
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            return await conn.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(sql, new { CheckingId = checkingId }, cancellationToken: ct));
        }
        public async Task SaveReviewAsync(Guid checkingId, string review, CancellationToken ct)
        {
            const string sql = """
            update public.objectstorage
            set review_res = @ReviewRes
            where checking_id = @CheckingId
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.QuerySingleOrDefaultAsync<string?>(
               new CommandDefinition(sql, new { CheckingId = checkingId, ReviewRes = review }, cancellationToken: ct));
        }

        public async Task SaveCompilationAsync(Guid checkingId, string compileRes, CancellationToken ct)
        {
            const string sql = """
            update public.objectstorage
            set compil_res = @CompilRes
            where checking_id = @CheckingId
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.QuerySingleOrDefaultAsync<string?>(
               new CommandDefinition(sql, new { CheckingId = checkingId, CompilRes = compileRes }, cancellationToken: ct));

        }

        public async Task<string?> ReadCompilationAsync(Guid checkingId, CancellationToken ct)
        {
            const string sql = """
            select compil_res
            from public.objectstorage
            where checking_id = @CheckingId
            """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            return await conn.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(sql, new { CheckingId = checkingId }, cancellationToken: ct));

        }
    }
}
