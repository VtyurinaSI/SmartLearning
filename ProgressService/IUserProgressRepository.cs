using static ProgressService.UserProgressRepository;

namespace ProgressService
{
    public interface IUserProgressRepository
    {
        Task<IReadOnlyList<ProgressRow>> GetUserProgressAsync(Guid userId, CancellationToken ct);
        Task<ProgressRow?> GetTaskProgressAsync(Guid userId, long taskId, CancellationToken ct);
        Task<Guid?> GetUserIdAsync(string userLogin, CancellationToken ct);
        Task SaveCheckingAsync(Guid userId, long taskId, string taskName, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, Guid? correlationId, bool isCheckingFinished, bool checkResult, string? compileMsg, string? testMsg, string? reviewMsg, CancellationToken ct);
    }
}
