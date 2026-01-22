namespace SmartLearning.Contracts
{
    public record UserCreated(Guid UserId, string Login, string Email);
    public record StartCheckRequest(Guid UserId, long TaskId);
    public record UpdateProgress(Guid UserId, long TaskId, string? TaskName, bool IsCompiledSuccess, bool IsTestedSuccess, bool IsReviewedSuccess, Guid? CorrelationId, bool IsCheckingFinished, bool CheckResult, string? CompileMsg, string? TestMsg, string? ReviewMsg);
    public record StartChecking(Guid CorrelationId, Guid UserId, long TaskId, string TaskName);
    public record ReceivedForChecking(long TaskId, string OrigCode);
    public record CheckingResults(Guid UserId, Guid CorrelationId, string? CompileRes, string? TestsRes, string? ReviewRes);

    public record UserProgressRow(Guid UserId, long TaskId, string TaskName, Guid? CorrelationId, bool IsCheckingFinished, bool CheckResult,
                                  bool CompileStat, string? CompileMsg,
                                  bool TestStat, string? TestMsg,
                                  bool ReviewStat, string? ReviewMsg);

    public record StartCompile(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartTests(Guid CorrelationId, Guid UserId, long TaskId);
    public record StartReview(Guid CorrelationId, Guid UserId, long TaskId);
    public record Cancel(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewRequested(Guid CorrelationId, Guid UserId, long TaskId, string PatternName);
    public record CompileRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestRequested(Guid CorrelationId, Guid UserId, long TaskId);
    public record CompilationFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record CompilationFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record WrongProjectStructure(Guid CorrelationId, Guid UserId, long TaskId, string? Message);
    public record CompileTimeout(Guid CorrelationId, Guid UserId, long TaskId);
    public record TestsFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record TestsFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record TestsTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record ReviewFinished(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record ReviewFailed(Guid CorrelationId, Guid UserId, long TaskId, string? Result);
    public record ReviewTimeout(Guid CorrelationId, Guid UserId, long TaskId);

    public record Finalize(Guid CorrelationId, Guid UserId, long TaskId);
}

