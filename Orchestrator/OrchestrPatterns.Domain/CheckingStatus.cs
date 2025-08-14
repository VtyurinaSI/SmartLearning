namespace OrchestrPatterns.Domain
{
    public enum CheckingStatus
    {
        Created,
        Compiling,
        Compiled,
        Testing,
        Tested,
        Reviewing,
        Reviewed,
        Canceled,
        Failed,
        Passed
    }
}
