namespace MinIoStub
{
    public interface IObjectStorageRepository
    {
        Task<Guid> SaveOrigCodeAsync(string origCode, Guid userId, CancellationToken ct);
        Task<string?> ReadReviewAsync(Guid checkingId, CancellationToken ct);
        Task<string?> ReadOrigCodeAsync(Guid checkingId, CancellationToken ct);
        Task SaveReviewAsync(Guid checkingId, string review, CancellationToken ct);
        Task SaveCompilationAsync(Guid checkingId, string compileRes, CancellationToken ct);
        Task<string?> ReadCompilationAsync(Guid checkingId, CancellationToken ct);
    }
}
