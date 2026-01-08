using System.Text.Json.Serialization;

public sealed record TokenResponse([property: JsonPropertyName("token")] string Token);
