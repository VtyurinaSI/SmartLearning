namespace ReflectionService.Domain
{
    public sealed record StepResult(
        string StepId,
        string Operation,
        bool Passed,
        FailureSeverity Severity,
        string Message,
        string? Details = null
        );
}
