namespace ReflectionService.Domain.Steps.FindInterface;

public sealed record FindInterfacesArgs(
    TypeVisibility Visibility = TypeVisibility.Any,
    string? NameRegex = null,
    string? NamespaceRegex = null,
    bool? ExcludeCompilerGenerated = null
);
