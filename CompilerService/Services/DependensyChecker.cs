namespace CompilerService.Services
{
    public class DependensyChecker
    {
        public DependensyChecker(CsprojParser _xmlParser)
        {
            сsprojParser = _xmlParser;
        }
        private readonly CsprojParser сsprojParser;
        public (string? mainProgPath, string? userMassage) SelectTargetProjectByInDegree(IReadOnlyList<string> projects)
        {
            Dictionary<string, int> nodes = new();
            foreach (var project in projects)
            {
                if (!IsTest(project))
                    nodes[NormalizePath(project)] = 0;
            }

            foreach (var curNode in nodes.Keys.ToArray())
            {
                var curDir = Path.GetDirectoryName(curNode)!;
                string[] dependencies = сsprojParser.GetDependencies(curNode);
                foreach (var dep in dependencies)
                {
                    var depAbs = NormalizePath(Path.Combine(curDir, dep));
                    if (nodes.ContainsKey(depAbs))
                        nodes[depAbs]++;
                }
            }
            var roots = nodes.Where(n => n.Value == 0).OrderBy(r => r.Key, StringComparer.Ordinal).ToList();
            if (roots.Count == 0)
                return (null, "Не найден корневой проект");

            if (roots.Count > 1)
                return (roots[0].Key, $"Найдено {roots.Count} корневых проектов. Для сборки выбран: {Path.GetFileName(roots[0].Key)}");

            return (roots[0].Key, null);
        }

        private readonly string[] testPackages = ["Microsoft.NET.Test.Sdk", "xunit", "nunit", "MSTest"];
        private bool IsTest(string path)
            => сsprojParser.GetPackages(path)
            .Any(proj => testPackages
                    .Any(pack => proj.Contains(pack, StringComparison.OrdinalIgnoreCase)));

        private string NormalizePath(string path)
            => Path.GetFullPath(path);
    }
}
