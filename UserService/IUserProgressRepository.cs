using SmartLearning.Contracts;

public interface IUserProgressRepository
{
    Task CreateUserAsync(UserCreated user, CancellationToken ct);
    Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken ct);
    Task<bool> UpdateUserProfileAsync(Guid userId, string? location, string? programmingLanguage, CancellationToken ct);
    Task<string> GetUserRoleAsync(Guid userId, CancellationToken ct);
    Task SetUserRoleAsync(Guid userId, string role, CancellationToken ct);

}
