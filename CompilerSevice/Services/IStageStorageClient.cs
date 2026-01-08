using SmartLearning.FilesUtils;

namespace CompilerSevice;

public interface IStageStorageClient
{
    Task<SourceStageLoader.StorageDownload> DownloadStageAsync(Guid userId, long taskId, string stage, string? fileName, CancellationToken ct);
    Task UploadStageAsync(Guid userId, long taskId, string stage, string fileName, byte[] bytes, string contentType, CancellationToken ct);
}
