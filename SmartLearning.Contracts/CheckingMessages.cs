namespace SmartLearning.Contracts
{
    public record StartCompile(Guid CorrelationId);
    public record StartTests(Guid CorrelationId);
    public record StartReview(Guid CorrelationId);
    public record Cancel(Guid CorrelationId);

    public record ReviewRequested(Guid CorrelationId);
    public record CodeCompiled(Guid CorrelationId);
    public record CompilationFailed(Guid CorrelationId);
    public record CompileTimeout(Guid CorrelationId);
    public record StartMqDto(bool SkipCompile = false, bool SkipTests = false, Guid CorrelationId = default);
    public record TestsFinished(Guid CorrelationId);
    public record TestsFailed(Guid CorrelationId);
    public record TestsTimeout(Guid CorrelationId);

    public record ReviewFinished(Guid CorrelationId);
    public record ReviewFailed(Guid CorrelationId);
    public record ReviewTimeout(Guid CorrelationId);

    public record Finalize(Guid CorrelationId);
}
