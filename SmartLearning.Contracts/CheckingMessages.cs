namespace SmartLearning.Contracts
{
    public record StartChecking(Guid CorrelationId, Guid UserId, long TaskId);
    public record RecievedForChecking(string UserLogin, long TaskId, string OrigCode);
    public record CheckingResults(Guid CorrelationId, string? CompilRes, string? TestsRes, string? ReviewRes);

    public record StartCompile(Guid CorrelationId);
    public record StartTests(Guid CorrelationId);
    public record StartReview(Guid CorrelationId);
    public record Cancel(Guid CorrelationId);

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
