namespace ReflectionService.Domain.Steps.FindCtor;

public sealed record FindCtorConsumersArgs(
    TypeVisibility Visibility = TypeVisibility.Any,
    bool? ExcludeCompilerGenerated = null
);
