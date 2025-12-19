using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

public sealed record ManifestStep
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    [JsonPropertyName("input")]
    public string? InputRole { get; init; }

    [JsonPropertyName("output")]
    public string? OutputRole { get; init; }

    [JsonPropertyName("args")]
    public JsonElement Args { get; init; }

    [JsonPropertyName("onFail")]
    public StepFailurePolicy OnFail { get; init; } = new(FailureSeverity.Error, "Step failed.");

    [JsonPropertyName("stopOnFail")]
    public bool StopOnFail { get; init; } = false;
}
