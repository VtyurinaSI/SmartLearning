using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using SmartLearning.Contracts;
using System.Threading.Tasks;

namespace UserService
{
    public class UserProgressRepository : IUserProgressRepository
    {
        private readonly NpgsqlDataSource _ds;
        private readonly ILogger<UserProgressRepository> _log;

        public UserProgressRepository(NpgsqlDataSource ds, ILogger<UserProgressRepository> log)
        {
            _ds = ds;
            _log = log;
        }


        public async Task CreateUserAsync(UserCreated user, CancellationToken ct)
        {
            const string sql = """
                insert into public.users(user_id, user_login, email)
                values (@UserId, @Login, @Email)
                on conflict (user_id) do update
                  set user_login = excluded.user_login, email = excluded.email;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.QuerySingleOrDefaultAsync<string?>(
               new CommandDefinition(
                   sql,
                   new
                   {
                       UserId = user.UserId,
                       Login = user.Login,
                       Email = user.Email
                   },
                   cancellationToken: ct));
        }

        public async Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken ct)
        {
            const string sql = """
                select
                  u.user_id as Id,
                  u.user_login as Login,
                  u.email as Email,
                  u.user_loc as Location,
                  u.user_prog_lang as ProgrammingLanguage,
                  coalesce(r.role, 'user') as Role
                from public.users u
                left join public.user_roles r on r.user_id = u.user_id
                where u.user_id = @UserId;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            return await conn.QuerySingleOrDefaultAsync<UserProfile>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        }

        public async Task<bool> UpdateUserProfileAsync(Guid userId, string? location, string? programmingLanguage, CancellationToken ct)
        {
            const string sql = """
                update public.users
                set user_loc = coalesce(@Location, user_loc),
                    user_prog_lang = coalesce(@ProgrammingLanguage, user_prog_lang)
                where user_id = @UserId;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { UserId = userId, Location = location, ProgrammingLanguage = programmingLanguage },
                    cancellationToken: ct));

            return rows > 0;
        }

        public async Task<string> GetUserRoleAsync(Guid userId, CancellationToken ct)
        {
            const string sql = """
                select role
                from public.user_roles
                where user_id = @UserId;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            var role = await conn.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
            return string.IsNullOrWhiteSpace(role) ? "user" : role;
        }

        public async Task SetUserRoleAsync(Guid userId, string role, CancellationToken ct)
        {
            const string sql = """
                insert into public.user_roles(user_id, role)
                values (@UserId, @Role)
                on conflict (user_id) do update
                  set role = excluded.role;
                """;

            await using var conn = await _ds.OpenConnectionAsync(ct);
            await conn.ExecuteAsync(
                new CommandDefinition(sql, new { UserId = userId, Role = role }, cancellationToken: ct));
        }
    }
}

