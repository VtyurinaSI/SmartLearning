using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace UserService
{
    public sealed class DbBootstrapper : IDbBootstrapper
    {
        private readonly NpgsqlDataSource _ds;
        private readonly ILogger<DbBootstrapper> _log;

        public DbBootstrapper(NpgsqlDataSource ds, ILogger<DbBootstrapper> log)
        {
            _ds = ds;
            _log = log;
        }

        public async Task EnsureAsync(CancellationToken ct = default)
        {
            const string sql = """
                create table if not exists public.user_roles (
                  user_id uuid primary key,
                  role text not null,
                  constraint user_roles_user_fk
                    foreign key (user_id)
                    references public.users(user_id)
                    on delete cascade
                );
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            _log.LogInformation("User roles table ensured.");
        }
    }
}

