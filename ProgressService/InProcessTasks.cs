namespace ProgressService
{
    public record InProcessTasks(long TaskId, string TaskName, string NextCheckingStage, string Comment);
}
