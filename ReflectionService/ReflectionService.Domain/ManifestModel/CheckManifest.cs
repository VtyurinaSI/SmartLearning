using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

public sealed record CheckManifest
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("pattern")]
    public required string Pattern { get; init; }

    [JsonPropertyName("version")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public required string Version { get; init; }

    [JsonPropertyName("target")]
    public ManifestTarget Target { get; init; } = new();

    [JsonPropertyName("steps")]
    public required ManifestStep[] Steps { get; init; }
}
