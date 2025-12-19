using System.Text.Json.Serialization;

namespace ReflectionService.Domain.Steps.FindTypesStep
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TypeVisibility
    {
        Public,
        Internal,
        Any
    }
}
