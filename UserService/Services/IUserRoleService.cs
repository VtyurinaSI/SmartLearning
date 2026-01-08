namespace UserService
{
    public interface IUserRoleService
    {
        Task<bool> IsAdminAsync(Guid userId, CancellationToken ct);
        bool TryNormalizeRole(string? role, out string normalizedRole);
    }
}
