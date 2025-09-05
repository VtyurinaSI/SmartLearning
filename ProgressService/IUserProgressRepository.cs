namespace ProgressService
{
    public interface IUserProgressRepository
    {
        Task<Guid?> GetUserIdAsync(string userLogin, CancellationToken ct);
        Task SaveCheckingAsync(Guid userId, long taskId, bool isCompiledSuccess, bool isTestedSuccess, bool isReviewedSuccess, CancellationToken ct);
    }
}
