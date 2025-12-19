using System.Text.Json.Serialization;

namespace ReflectionService.Domain.Steps.FindTypesStep
{
    public sealed record FindTypesArgs
    {
        [JsonPropertyName("kind")]
        public required TypeKind Kind { get; init; }

        [JsonPropertyName("visibility")]
        public required TypeVisibility Visibility { get; init; }

        [JsonPropertyName("nameRegex")]
        public string? NameRegex { get; init; }

        [JsonPropertyName("namespaceRegex")]
        public string? NamespaceRegex { get; init; }

        [JsonPropertyName("excludeCompilerGenerated")]
        public bool? ExcludeCompilerGenerated { get; init; }
    }
}
