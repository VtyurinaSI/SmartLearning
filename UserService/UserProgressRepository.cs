using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using SmartLearning.Contracts;
using System.Threading.Tasks;

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
    }
}
