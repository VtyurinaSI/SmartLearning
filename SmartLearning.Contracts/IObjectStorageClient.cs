namespace SmartLearning.Contracts
{
    public interface IObjectStorageClient
    {
        Task WriteFile<T>(T data, Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token);
        Task<T> ReadFile<T>(Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token);
    }
}
