namespace ReflectionService.Domain.Steps.FindImplementations;

public sealed record FindImplementationsArgs(
    TypeVisibility Visibility = TypeVisibility.Any,
    bool IncludeAbstract = false,
    bool? ExcludeCompilerGenerated = null
);
