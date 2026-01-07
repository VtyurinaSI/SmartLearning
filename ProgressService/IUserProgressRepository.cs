using static ProgressService.UserProgressRepository;

namespace ProgressService
{
    public interface IUserProgressRepository
    {
        Task<IReadOnlyList<ProgressRow>> GetUserProgressAsync(Guid userId, CancellationToken ct);
        Task<Guid?> GetUserIdAsync(string userLogin, CancellationToken ct);
        Task SaveCheckingAsync(Guid userId, long taskId, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, Guid? correlationId, bool checkResult, string? compileMsg, string? testMsg, string? reviewMsg, CancellationToken ct);
    }
}
