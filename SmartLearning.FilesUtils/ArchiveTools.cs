using System.IO.Compression;

namespace SmartLearning.FilesUtils;

public static class ArchiveTools
{
    public static ExtractInfo ExtractZipToDirectory(byte[] zipBytes, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var destFull = Path.GetFullPath(destinationDir) + Path.DirectorySeparatorChar;

        long totalUncompressed = 0;
        int files = 0;
        int dirs = 0;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
            {
                var dirPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));
                if (!dirPath.StartsWith(destFull, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Unsafe zip entry path: {entry.FullName}");

                Directory.CreateDirectory(dirPath);
                dirs++;
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));
            if (!fullPath.StartsWith(destFull, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unsafe zip entry path: {entry.FullName}");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);

            files++;
            totalUncompressed += entry.Length;
        }

        return new ExtractInfo(archive.Entries.Count, files, dirs, totalUncompressed);
    }

    public sealed record ExtractInfo(int Entries, int Files, int Dirs, long TotalUncompressedBytes);
}
