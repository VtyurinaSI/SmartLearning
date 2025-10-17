namespace GatewayPatterns
{
    public interface IObjectStorageClient
    {
        Task<Guid> SaveOrigCodeAsync(string origCode, Guid userId, Guid checkingId, CancellationToken ct);
        Task SaveReviewAsync(Guid checkingId, string review, CancellationToken ct);
        Task<string?> ReadOrigCodeAsync(Guid checkingId, CancellationToken ct);
        Task<string?> ReadReviewAsync(Guid checkingId, CancellationToken ct);
    }
}
