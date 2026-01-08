namespace GatewayPatterns
{
    public sealed class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
    }
}
