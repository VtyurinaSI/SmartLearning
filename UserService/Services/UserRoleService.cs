using Microsoft.Extensions.Options;

namespace UserService
{
    public sealed class UserRoleService : IUserRoleService
    {
        private readonly IUserProgressRepository _repo;
        private readonly HashSet<string> _allowedRoles;

        public UserRoleService(IUserProgressRepository repo, IOptions<UserRoleOptions> options)
        {
            _repo = repo;
            var roles = options.Value.AllowedRoles ?? Array.Empty<string>();
            _allowedRoles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct)
        {
            var role = await _repo.GetUserRoleAsync(userId, ct);
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryNormalizeRole(string? role, out string normalizedRole)
        {
            var trimmed = role?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !_allowedRoles.Contains(trimmed))
            {
                normalizedRole = string.Empty;
                return false;
            }

            normalizedRole = trimmed.ToLowerInvariant();
            return true;
        }
    }
}
