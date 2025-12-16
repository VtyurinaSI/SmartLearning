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

    public record ReviewRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompileRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompilationFinished(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompilationFailed(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompileTimeout(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestsFinished(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestsFailed(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestsTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewFinished(Guid CorrelationId, Guid UserId, long TaskId);
    public record ReviewFailed(Guid CorrelationId, Guid UserId, long TaskId);
    public record ReviewTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record Finalize(Guid CorrelationId, Guid UserId, long TaskId);
}
