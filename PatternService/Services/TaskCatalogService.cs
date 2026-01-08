using Microsoft.Extensions.Options;
using System.Text;

namespace PatternService;

public sealed class TaskCatalogService
{
    private readonly ITaskCatalogRepository _repo;
    private readonly IContentStorage _storage;
    private readonly CatalogOptions _options;

    public TaskCatalogService(ITaskCatalogRepository repo, IContentStorage storage, IOptions<CatalogOptions> options)
    {
        _repo = repo;
        _storage = storage;
        _options = options.Value;
    }

    public Task<TaskMeta?> GetMetaAsync(long taskId, CancellationToken ct)
        => _repo.GetMetaAsync(taskId, ct);

    public async Task<IReadOnlyList<TaskListItem>> GetTaskListAsync(CancellationToken ct)
    {
        var metas = await _repo.GetAllMetaAsync(ct);
        var items = await Task.WhenAll(metas.Select(meta => BuildItemAsync(meta, ct)));
        return items;
    }

    public Task<CatalogFileResult?> GetTheoryAsync(long taskId, CancellationToken ct)
        => GetFileAsync(taskId, _options.TheoryFileName, "text/markdown; charset=utf-8", ct);

    public Task<CatalogFileResult?> GetTaskAsync(long taskId, CancellationToken ct)
        => GetFileAsync(taskId, _options.TaskFileName, "text/markdown; charset=utf-8", ct);

    public Task<CatalogFileResult?> GetManifestAsync(long taskId, CancellationToken ct)
        => GetFileAsync(taskId, _options.ManifestFileName, "application/json", ct);

    private async Task<TaskListItem> BuildItemAsync(TaskMeta meta, CancellationToken ct)
    {
        var key = BuildKey(_options.BasePrefix, meta.TaskId, meta.Version, _options.TaskFileName);
        var bytes = await _storage.GetAsync(key, ct);
        var text = bytes is null ? string.Empty : Encoding.UTF8.GetString(bytes);
        text = text.TrimStart('\uFEFF');
        var max = _options.SnippetLength;
        var tail = max > 3 ? max - 3 : 0;
        var snippet = text.Length > max ? text[..tail] + "..." : text;
        return new TaskListItem(meta.TaskId, meta.PatternTitle, snippet);
    }

    private async Task<CatalogFileResult?> GetFileAsync(long taskId, string fileName, string contentType, CancellationToken ct)
    {
        var meta = await _repo.GetMetaAsync(taskId, ct);
        if (meta is null)
        {
            return null;
        }

        var key = BuildKey(_options.BasePrefix, taskId, meta.Version, fileName);
        var bytes = await _storage.GetAsync(key, ct);
        return bytes is null ? null : new CatalogFileResult(bytes, fileName, contentType);
    }

    private static string BuildKey(string basePrefix, long taskId, int version, string fileName)
        => $"{basePrefix}/{taskId}/v{version}/{fileName}";
}
