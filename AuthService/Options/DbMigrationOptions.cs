namespace AuthService
{
    public sealed class DbMigrationOptions
    {
        public int MaxAttempts { get; set; } = 10;
        public int DelayStepSeconds { get; set; } = 2;
        public int MaxDelaySeconds { get; set; } = 10;
    }
}
