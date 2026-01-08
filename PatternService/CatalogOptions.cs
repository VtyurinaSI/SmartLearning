namespace PatternService;

public sealed class CatalogOptions
{
    public string BasePrefix { get; init; } = "tasks";
    public int Version { get; init; } = 1;
    public string TheoryFileName { get; init; } = "theory.md";
    public string TaskFileName { get; init; } = "task.md";
    public string ManifestFileName { get; init; } = "manifest.json";
    public int SnippetLength { get; init; } = 100;
}
