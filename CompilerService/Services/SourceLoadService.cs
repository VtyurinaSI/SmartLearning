using SmartLearning.FilesUtils;

namespace CompilerService;

public sealed class SourceLoadService
{
    private readonly IStageStorageClient _storage;

    public SourceLoadService(IStageStorageClient storage)
    {
        _storage = storage;
    }

    public Task<SourceStageLoader.SourceLoadResult> LoadAsync(
        Guid userId,
        long taskId,
        Guid correlationId,
        CancellationToken ct)
    {
        return SourceStageLoader.LoadAsync(
            download: downloadCt => _storage.DownloadStageAsync(userId, taskId, "load", null, downloadCt),
            correlationId: correlationId,
            ct: ct);
    }
}

