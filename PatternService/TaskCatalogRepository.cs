using Dapper;
using System.Data;

namespace PatternService;

public sealed class TaskCatalogRepository(IDbConnection db) : ITaskCatalogRepository
{
    public async Task<TaskMeta?> GetMetaAsync(long taskId, CancellationToken ct)
    {
        var sql = """
select
  t.task_id       as TaskId,
  t.title         as TaskTitle,
  p.pattern_key   as PatternKey,
  p.title         as PatternTitle,
  t.current_version as Version
from public.tasks t
join public.patterns p on p.pattern_key = t.pattern_key
where t.task_id = @taskId
""";

        var cmd = new CommandDefinition(sql, new { taskId }, cancellationToken: ct);
        return await db.QuerySingleOrDefaultAsync<TaskMeta>(cmd);
    }
}
