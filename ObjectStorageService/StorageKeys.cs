namespace ObjectStorageService
{
    public static class StorageKeys
    {
        public static string StageSegment(CheckStage stage) => stage switch
        {
            CheckStage.Load => "00-load",
            CheckStage.Build => "01-build",
            CheckStage.Reflect => "02-reflect",
            CheckStage.Llm => "03-llm",
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };

        public static string Base(Guid userId, long taskId)
            => $"submissions/{userId}/{taskId}";

        public static string StagePrefix(Guid userId, long taskId, CheckStage stage)
            => $"{Base(userId, taskId)}/{StageSegment(stage)}";

        public static string File(Guid userId, long taskId, CheckStage stage, string name)
            => $"{StagePrefix(userId, taskId, stage)}/{name}";
    }
}
