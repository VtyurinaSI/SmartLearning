public sealed class UserProfile
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? ProgrammingLanguage { get; set; }
    public string Role { get; set; } = "user";
}

public sealed class UpdateProfileRequest
{
    public string? Location { get; set; }
    public string? ProgrammingLanguage { get; set; }
}

public sealed class SetUserRoleRequest
{
    public string? Role { get; set; }
}
