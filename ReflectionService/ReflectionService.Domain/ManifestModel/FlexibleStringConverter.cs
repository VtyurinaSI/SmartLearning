using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReflectionService.Domain.ManifestModel;

public sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => GetRawTokenString(ref reader),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    private static string GetRawTokenString(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
            return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());

        return Encoding.UTF8.GetString(reader.ValueSpan);
    }
}
