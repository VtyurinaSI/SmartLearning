namespace LlmService;

public sealed class ReviewFileCollector
{
    public List<string> CollectRelevantFiles(string rootDir)
    {
        static bool IsExcluded(string path)
        {
            var p = path.Replace('\\', '/');
            return p.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.git/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.vs/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.idea/", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsRelevant(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)) return true;
            if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) return true;

            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return false;
                if (name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }

            return false;
        }

        var all = Directory
            .EnumerateFiles(rootDir, "*", SearchOption.AllDirectories)
            .Where(p => !IsExcluded(p))
            .Where(IsRelevant)
            .ToList();

        int Priority(string p)
        {
            var name = Path.GetFileName(p);
            var ext = Path.GetExtension(p).ToLowerInvariant();

            if (ext == ".sln") return 0;
            if (ext == ".csproj") return 1;
            if (ext == ".cs" && name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)) return 3;
            if (ext == ".cs") return 2;
            return 9;
        }

        return all
            .OrderBy(Priority)
            .ThenBy(p => Path.GetRelativePath(rootDir, p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
