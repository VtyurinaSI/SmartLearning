using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record class LoginRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    [JsonPropertyName("login")] public string Login { get; init; } = default!;

    [Required, StringLength(100, MinimumLength = 6)]
    [JsonPropertyName("password")] public string Password { get; init; } = default!;
}
