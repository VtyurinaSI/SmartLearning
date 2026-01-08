using SmartLearning.FilesUtils;

namespace LlmService;

public sealed class ReviewSourceLoader
{
    private readonly IReviewStorageClient _storage;

    public ReviewSourceLoader(IReviewStorageClient storage)
    {
        _storage = storage;
    }

    public async Task<ReviewSourceResult> LoadAsync(Guid userId, long taskId, Guid correlationId, CancellationToken ct)
    {
        var swDownload = System.Diagnostics.Stopwatch.StartNew();
        var dl = await _storage.DownloadStageAsync(userId, taskId, "load", null, ct);
        swDownload.Stop();

        if (dl.Bytes.Length == 0)
            throw new InvalidOperationException("No sources found in load stage");

        var workDir = CreateWorkDir(correlationId);
        RecreateDir(workDir);

        var isZip =
            IsZip(dl.Bytes) ||
            dl.ContentType.Contains("zip", StringComparison.OrdinalIgnoreCase) ||
            dl.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

        ArchiveTools.ExtractInfo? extractInfo = null;
        TimeSpan? extractTime = null;

        if (isZip)
        {
            var swExtract = System.Diagnostics.Stopwatch.StartNew();
            extractInfo = ArchiveTools.ExtractZipToDirectory(dl.Bytes, workDir);
            swExtract.Stop();
            extractTime = swExtract.Elapsed;
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(dl.FileName) ? "source.txt" : dl.FileName;
            var safe = SanitizeFileName(name);
            var path = Path.Combine(workDir, safe);
            await File.WriteAllBytesAsync(path, dl.Bytes, ct);
        }

        return new ReviewSourceResult(
            workDir,
            isZip,
            dl.Bytes.Length,
            dl.ContentType,
            dl.FileName,
            swDownload.Elapsed,
            extractTime,
            extractInfo);
    }

    private static string CreateWorkDir(Guid correlationId)
    {
        var root = GetEnv("WORK_ROOT", "");
        if (string.IsNullOrWhiteSpace(root))
            root = Path.GetTempPath();

        return Path.Combine(root, "smartlearning", "review", correlationId.ToString("N"));
    }

    private static bool IsZip(byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B;

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static void RecreateDir(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    private static string GetEnv(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;
}

public sealed record ReviewSourceResult(
    string WorkDir,
    bool IsZip,
    long Bytes,
    string ContentType,
    string FileName,
    TimeSpan DownloadTime,
    TimeSpan? ExtractTime,
    ArchiveTools.ExtractInfo? ExtractInfo);
