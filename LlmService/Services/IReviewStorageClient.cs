namespace LlmService;

public interface IReviewStorageClient
{
    Task<StorageDownload> DownloadStageAsync(Guid userId, long taskId, string stage, string? fileName, CancellationToken ct);
    Task UploadStageAsync(Guid userId, long taskId, string stage, string fileName, byte[] bytes, string mediaType, string? charset, CancellationToken ct);
}
