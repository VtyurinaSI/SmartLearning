namespace SmartLearning.Contracts
{
    public record UserCreated(Guid UserId, string Login, string Email);
    // UpdateProgress now carries correlation id, final check result and optional messages for each stage
    public record UpdateProgress(Guid UserId, long TaskId, bool IsCompiledSuccess, bool IsTestedSuccess, bool IsReviewedSuccess, Guid? CorrelationId, bool CheckResult, string? CompileMsg, string? TestMsg, string? ReviewMsg);
    public record StartChecking(Guid CorrelationId, Guid UserId, long TaskId);
    public record RecievedForChecking(long TaskId, string OrigCode);
    public record CheckingResults(Guid UserId, Guid CorrelationId, string? CompilRes, string? TestsRes, string? ReviewRes);

    // DTO that mirrors the public.progress table row structure
    // Order changed to: CorrelationId, CheckResult, CompileStat, CompileMsg, TestStat, TestMsg, ReviewStat, ReviewMsg
    public record UserProgressRow(Guid UserId, long TaskId, string TaskName, Guid? CorrelationId, bool CheckResult,
                                  bool CompileStat, string? CompileMsg,
                                  bool TestStat, string? TestMsg,
                                  bool ReviewStat, string? ReviewMsg);

    public record StartCompile(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartTests(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartReview(Guid CorrelationId, Guid UserId, long TaskId);
    public record Cancel(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompileRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompilationFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record CompilationFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record CompileTimeout(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestsFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record TestsFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record TestsTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record ReviewFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record ReviewTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record Finalize(Guid CorrelationId, Guid UserId, long TaskId);
}
