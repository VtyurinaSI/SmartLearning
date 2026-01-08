namespace GatewayPatterns
{
    public sealed class DownstreamOptions
    {
        public string Users { get; set; } = string.Empty;
        public string Llm { get; set; } = string.Empty;
        public string Orch { get; set; } = string.Empty;
        public string Progress { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string Patterns { get; set; } = string.Empty;
    }
}
