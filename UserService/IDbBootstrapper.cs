public interface IDbBootstrapper
{
    Task EnsureAsync(CancellationToken ct = default);
}
