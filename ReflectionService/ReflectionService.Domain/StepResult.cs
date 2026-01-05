namespace ReflectionService.Domain
{
    public sealed record StepResult(
        string StepId,
        string Operation,
        bool Passed = false,
        FailureSeverity? Severity = null,
        string? Message = null,
        string? Details = null
        );
}
