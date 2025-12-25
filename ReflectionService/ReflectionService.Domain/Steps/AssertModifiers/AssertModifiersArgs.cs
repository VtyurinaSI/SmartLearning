namespace ReflectionService.Domain.Steps.AssertModifiers;

public sealed record AssertModifiersArgs(
    bool? Sealed = null,
    bool? Abstract = null,
    bool? StaticClass = null,
    TypeVisibility? Visibility = null
);
