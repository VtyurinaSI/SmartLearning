namespace UserService
{
    public sealed class UserRoleOptions
    {
        public string[] AllowedRoles { get; set; } = new[] { "user", "admin" };
    }
}
