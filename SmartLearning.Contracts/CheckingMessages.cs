namespace SmartLearning.Contracts
{
    public record UserCreated(Guid UserId, string Login, string Email);
    public record UpdateProgress(Guid UserId, long TaskId, bool IsCompiledSuccess, bool IsTestedSuccess, bool IsReviewedSuccess);
    public record StartChecking(Guid CorrelationId, Guid UserId, long TaskId);
    public record RecievedForChecking(long TaskId, string OrigCode);
    public record CheckingResults(Guid UserId, Guid CorrelationId, string? CompilRes, string? TestsRes, string? ReviewRes);

    public record StartCompile(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartTests(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartReview(Guid CorrelationId, Guid UserId, long TaskId);
    public record Cancel(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewRequested(Guid CorrelationId);
    public record CompileRequested(Guid CorrelationId);
    public record CompilationFinished(Guid CorrelationId);
    public record CompilationFailed(Guid CorrelationId);
    public record CompileTimeout(Guid CorrelationId);
    public record TestsFinished(Guid CorrelationId);
    public record TestsFailed(Guid CorrelationId);
    public record TestsTimeout(Guid CorrelationId);

    public record ReviewFinished(Guid CorrelationId);
    public record ReviewFailed(Guid CorrelationId);
    public record ReviewTimeout(Guid CorrelationId);

    public record Finalize(Guid CorrelationId);
}
