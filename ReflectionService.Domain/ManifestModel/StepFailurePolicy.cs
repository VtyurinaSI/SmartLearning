using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

public sealed record StepFailurePolicy(
    [property: JsonPropertyName("severity")] FailureSeverity Severity,
    [property: JsonPropertyName("message")] string Message
);
