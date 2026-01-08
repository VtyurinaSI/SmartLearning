namespace ProgressService
{
    public record UserProgress(ComplitedTasks[] ComplitedTasks, InProcessTasks[] InProcessTasks);
}
