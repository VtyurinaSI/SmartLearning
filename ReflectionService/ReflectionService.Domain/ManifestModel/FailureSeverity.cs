using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FailureSeverity
{
    Error,
    Warning
}
