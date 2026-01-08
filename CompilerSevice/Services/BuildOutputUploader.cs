namespace CompilerSevice;

public sealed class BuildOutputUploader
{
    private readonly IStageStorageClient _storage;
    private readonly int _maxConcurrency = 4;

    public BuildOutputUploader(IStageStorageClient storage)
    {
        _storage = storage;
    }

    public async Task UploadBuildOutputAsync(Guid userId, long taskId, string outDir, CancellationToken ct)
    {
        var files = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);

        using var gate = new SemaphoreSlim(_maxConcurrency);

        var uploads = files.Select(async path =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var rel = Path.GetRelativePath(outDir, path).Replace('\\', '/');
                var bytes = await File.ReadAllBytesAsync(path, ct);

                await _storage.UploadStageAsync(
                    userId,
                    taskId,
                    "build",
                    rel,
                    bytes,
                    "application/octet-stream",
                    ct);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(uploads);
    }
}
