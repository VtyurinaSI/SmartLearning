namespace CompilerService;

public sealed class BuildTargetLocator
{
    public TargetsInfo FindTargets(string root)
    {
        var solutions = Directory.GetFiles(root, "*.sln", SearchOption.AllDirectories);
        var projects = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);

        return new TargetsInfo(solutions, projects);
    }

    public string FindBuildTarget(string root)
    {
        var sln = Directory.GetFiles(root, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
        if (sln is not null)
            return sln;

        var proj = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
        if (proj is not null)
            return proj;

        throw new InvalidOperationException("No build target found");
    }

    public sealed record TargetsInfo(IReadOnlyList<string> Solutions, IReadOnlyList<string> Projects);
}

