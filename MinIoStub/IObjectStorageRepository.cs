namespace MinIoStub
{
    public interface IObjectStorageRepository
    {
        Task<Guid> SaveSourceAsync(string origCode, CancellationToken ct);
        Task<string?> ReadReviewAsync(Guid checkingId, CancellationToken ct);
        Task<string?> ReadOrigCodeAsync(Guid checkingId, CancellationToken ct);
        Task<string?> SaveReviewAsync(Guid checkingId, string review, CancellationToken ct);
    }
}
