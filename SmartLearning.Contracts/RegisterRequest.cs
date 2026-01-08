using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
