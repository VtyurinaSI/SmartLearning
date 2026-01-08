using System.Text;

namespace LlmService;

public sealed class ReviewProjectStructureBuilder
{
    public string Build(string rootDir, IReadOnlyList<string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Project structure:");
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(rootDir, f).Replace('\\', '/');
            sb.Append("- ").AppendLine(rel);
        }
        return sb.ToString();
    }
}
