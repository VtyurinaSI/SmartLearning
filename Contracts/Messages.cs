namespace Contracts
{
    public record SubmissionRequested(
     Guid CorrelationId,
     string UserId,
     string Code,
     bool SkipCompile = false,
     bool SkipCheck = false,
     bool SkipReview = false);

    public record CompileRequested(Guid CorrelationId, string Code);
    public record CompileCompleted(Guid CorrelationId, bool Success, string? Output = null, string? Error = null);

    public record CodeCheckRequested(Guid CorrelationId, string Code);
    public record CodeCheckCompleted(Guid CorrelationId, bool Success, string? Report = null);

    public record ReviewRequested(Guid CorrelationId, string Code);
    public record ReviewCompleted(Guid CorrelationId, bool Success, string? Notes = null);

    public record StageFailed(Guid CorrelationId, string Stage, string Reason);

    public record OrchestrationCompleted(Guid CorrelationId, bool Success);

    public record TimeoutExpired(Guid CorrelationId, string Stage);
}
