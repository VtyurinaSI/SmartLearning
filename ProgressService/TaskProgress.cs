namespace ProgressService
{
    public record TaskProgress(long TaskId, string TaskName, bool IsCheckingFinished, bool CheckResult,
        bool CompileStat, string? CompileMsg,
        bool TestStat, string? TestMsg,
        bool ReviewStat, string? ReviewMsg);
}
