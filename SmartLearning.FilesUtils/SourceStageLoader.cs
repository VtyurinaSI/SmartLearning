using System.Diagnostics;

namespace SmartLearning.FilesUtils;

public static class SourceStageLoader
{
    public static async Task<SourceLoadResult> LoadAsync(
        Func<CancellationToken, Task<StorageDownload>> download,
        Guid correlationId,
        CancellationToken ct)
    {
        var swDownload = Stopwatch.StartNew();
        var dl = await download(ct);
        swDownload.Stop();

        if (dl.Bytes.Length == 0)
            throw new InvalidOperationException("No sources found in load stage");

        var isZip =
            IsZip(dl.Bytes) ||
            dl.ContentType.Contains("zip", StringComparison.OrdinalIgnoreCase) ||
            dl.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

        var workDir = Path.Combine(
            Path.GetTempPath(),
            "compile",
            correlationId.ToString("N"));

        RecreateDir(workDir);

        ArchiveTools.ExtractInfo? extractInfo = null;
        TimeSpan? extractTime = null;

        if (isZip)
        {
            var swExtract = Stopwatch.StartNew();
            extractInfo = ArchiveTools.ExtractZipToDirectory(dl.Bytes, workDir);
            swExtract.Stop();
            extractTime = swExtract.Elapsed;
        }

        return new SourceLoadResult(
            workDir,
            isZip,
            dl.Bytes.Length,
            dl.ContentType,
            dl.FileName,
            swDownload.Elapsed,
            extractTime,
            extractInfo);
    }

    private static bool IsZip(byte[] bytes)
        => bytes.Length >= 4 &&
           bytes[0] == 0x50 &&
           bytes[1] == 0x4B;

    private static void RecreateDir(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    public sealed record SourceLoadResult(
        string WorkDir,
        bool IsZip,
        long Bytes,
        string ContentType,
        string FileName,
        TimeSpan DownloadTime,
        TimeSpan? ExtractTime,
        ArchiveTools.ExtractInfo? ExtractInfo);

    public sealed record StorageDownload(
        byte[] Bytes,
        string ContentType,
        string FileName);
}
