
namespace Orchestrator.Domain
{
    public class CheckingData (Guid userId, long taskId, string? taskName)
    {
        public Guid UserId { get; set; } = userId;
        public long TaskId { get; set; } = taskId;
        public string? TaskName { get; set; } = taskName;
        public bool IsCompiledSuccess { get; set; } = false;
        public bool IsTestedSuccess { get; set; } = false;
        public bool IsReviewedSucces { get; set; } = false;
        public bool IsCheckingFinished { get; set; } = false;
        public bool CheckResult { get; set; }
        public string? CompileMsg { get; set; }
        public string? TestMsg { get; set; }
        public string? ReviewMsg { get; set; }

    }
}
