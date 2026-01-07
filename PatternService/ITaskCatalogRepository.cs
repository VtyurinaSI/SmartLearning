namespace PatternService;

public interface ITaskCatalogRepository
{
    Task<TaskMeta?> GetMetaAsync(long taskId, CancellationToken ct);
}
