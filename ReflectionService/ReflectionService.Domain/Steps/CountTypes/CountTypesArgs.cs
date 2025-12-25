namespace ReflectionService.Domain.Steps.CountTypes
{
    public sealed record CountTypesArgs(
       FindTypesStep.TypeKind Kind = FindTypesStep.TypeKind.Any,
       TypeVisibility Visibility = TypeVisibility.Any,
       string? NameRegex = null,
       string? NamespaceRegex = null,
       bool? ExcludeCompilerGenerated = null,
       int? Min = null,
       int? Max = null,
       int? Exact = null
   );
}
