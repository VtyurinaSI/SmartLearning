namespace ReflectionService.Domain.Steps.HasDependency;

[Flags]
public enum DependencyKinds
{
    Field = 1,
    Property = 2,
    ConstructorParam = 4,
    MethodParam = 8,
    Any = Field | Property | ConstructorParam | MethodParam
}

public sealed record HasDependencyArgs(
    string To = "self",
    DependencyKinds Kinds = DependencyKinds.Any,
    int? Min = null,
    TypeVisibility Visibility = TypeVisibility.Any,
    bool? ExcludeCompilerGenerated = null
);
