using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthService.DTOs;
public class LoginRequest
{
    [Required(ErrorMessage = "Логин обязателен")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
    [JsonPropertyName("login")]
    public required string Login { get; set; }
    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    [DataType(DataType.Password)]
    [JsonPropertyName("password")]
    public required string Password { get; set; }
    public LoginRequest() { }
    public LoginRequest(string login, string password)
    {
        Login = login;
        Password = password;
    }
}