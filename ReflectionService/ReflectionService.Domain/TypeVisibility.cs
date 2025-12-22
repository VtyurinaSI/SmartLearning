using System.Text.Json.Serialization;

namespace ReflectionService.Domain
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TypeVisibility
    {
        Public,
        Internal,
        Any
    }
}
