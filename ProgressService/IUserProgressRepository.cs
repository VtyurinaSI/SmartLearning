using static ProgressService.UserProgressRepository;

namespace ProgressService
{
    public interface IUserProgressRepository
    {
        Task<IReadOnlyList<ProgressRow>> GetUserProgressAsync(Guid userId, CancellationToken ct);
        Task<Guid?> GetUserIdAsync(string userLogin, CancellationToken ct);
        Task SaveCheckingAsync(Guid userId, long taskId, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, CancellationToken ct);
    }
}
