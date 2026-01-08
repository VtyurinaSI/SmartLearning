namespace ObjectStorageService
{
    public interface IStorageBootstrapper
    {
        Task EnsureAsync(CancellationToken ct = default);
    }
}
