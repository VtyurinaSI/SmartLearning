using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReflectionService.Domain
{
    public static class JsonOptions
    {
        public static readonly JsonSerializerOptions ManifestArgsConverterOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
        {
            new JsonStringEnumConverter()
        }
        };
    }
}
