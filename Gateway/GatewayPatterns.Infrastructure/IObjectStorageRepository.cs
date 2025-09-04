namespace GatewayPatterns.Infrastructure
{
    public interface IObjectStorageRepository
    {
        Task<Guid> SaveSourceAsync(string origCode, CancellationToken ct);
    }
}
