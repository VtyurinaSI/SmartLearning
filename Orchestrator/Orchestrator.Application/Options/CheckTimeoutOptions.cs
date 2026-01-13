namespace Orchestrator.Application
{
    public sealed class CheckTimeoutOptions
    {
        public int CompileMinutes { get; set; } = 2;
        public int TestMinutes { get; set; } = 2;
        public int ReviewMinutes { get; set; } = 10;
        public int PostCompileDelayMs { get; set; } = 200;
    }
}

