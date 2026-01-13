using System.Net;
using System.Net.Http.Json;

namespace Orchestrator.Application;

public sealed class PatternServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PatternServiceClient> _log;

    public PatternServiceClient(HttpClient http, ILogger<PatternServiceClient> log)
    {
        _http = http;
        _log = log;
    }

    private sealed record TaskMeta(string TaskTitle, string PatternTitle);

    public async Task<bool?> TaskExistsAsync(long taskId, CancellationToken ct)
    {
        var url = $"/meta?taskId={taskId}";
        using var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return false;

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("PatternService meta request failed: {StatusCode}", resp.StatusCode);
            return null;
        }

        return true;
    }

    public async Task<string?> GetPatternTitleAsync(long taskId, CancellationToken ct)
    {
        var url = $"/meta?taskId={taskId}";
        using var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("PatternService meta request failed: {StatusCode}", resp.StatusCode);
            return null;
        }

        try
        {
            var meta = await resp.Content.ReadFromJsonAsync<TaskMeta>(cancellationToken: ct);
            return meta?.PatternTitle;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "PatternService meta parsing failed for task {TaskId}", taskId);
            return null;
        }
    }

    public async Task<string?> GetTaskTitleAsync(long taskId, CancellationToken ct)
    {
        var url = $"/meta?taskId={taskId}";
        using var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("PatternService meta request failed: {StatusCode}", resp.StatusCode);
            return null;
        }

        try
        {
            var meta = await resp.Content.ReadFromJsonAsync<TaskMeta>(cancellationToken: ct);
            return meta?.TaskTitle;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "PatternService meta parsing failed for task {TaskId}", taskId);
            return null;
        }
    }
}

