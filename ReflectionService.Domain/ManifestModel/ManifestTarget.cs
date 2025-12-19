using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

public sealed record ManifestTarget
{
    [JsonPropertyName("assemblyName")]
    public string? AssemblyName { get; init; }

    [JsonPropertyName("entrypointTypeRegex")]
    public string? EntrypointTypeRegex { get; init; }

    [JsonPropertyName("excludeCompilerGenerated")]
    public bool ExcludeCompilerGenerated { get; init; } = true;
}
