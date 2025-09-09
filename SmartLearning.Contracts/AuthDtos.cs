using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record class LoginRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    [JsonPropertyName("login")] public string Login { get; init; } = default!;

    [Required, StringLength(100, MinimumLength = 6)]
    [JsonPropertyName("password")] public string Password { get; init; } = default!;
}

public sealed record class RegisterRequest
{
    [Required, EmailAddress]
    [JsonPropertyName("email")] public string Email { get; init; } = default!;

    [Required]
    [JsonPropertyName("firstName")] public string FirstName { get; init; } = default!;

    [Required]
    [JsonPropertyName("lastName")] public string LastName { get; init; } = default!;

    [Required, DataType(DataType.Password)]
    [JsonPropertyName("password")] public string Password { get; init; } = default!;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [JsonPropertyName("confirmPassword")] public string ConfirmPassword { get; init; } = default!;
}

public sealed record TokenResponse([property: JsonPropertyName("token")] string Token);