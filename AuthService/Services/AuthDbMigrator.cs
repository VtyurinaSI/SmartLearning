using AuthService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AuthService.Services
{
    public sealed class AuthDbMigrator
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthDbMigrator> _log;
        private readonly DbMigrationOptions _options;

        public AuthDbMigrator(AppDbContext db, ILogger<AuthDbMigrator> log, IOptions<DbMigrationOptions> options)
        {
            _db = db;
            _log = log;
            _options = options.Value;
        }

        public async Task EnsureMigratedAsync(CancellationToken ct)
        {
            for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
            {
                try
                {
                    await _db.Database.MigrateAsync(ct);
                    _log.LogInformation("EF Core migrations applied.");
                    break;
                }
                catch (NpgsqlException ex) when (attempt < _options.MaxAttempts)
                {
                    _log.LogWarning(ex, "DB not ready (attempt {Attempt}/{Max}). Waiting¢?³", attempt, _options.MaxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(_options.MaxDelaySeconds, attempt * _options.DelayStepSeconds)), ct);
                }
                catch (Exception ex) when (attempt < _options.MaxAttempts)
                {
                    _log.LogWarning(ex, "Migration failed (attempt {Attempt}/{Max}). Retrying¢?³", attempt, _options.MaxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(_options.MaxDelaySeconds, attempt * _options.DelayStepSeconds)), ct);
                }
            }
        }
    }
}
