namespace Orchestrator.Domain
{
    public enum CheckingStatus
    {
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

