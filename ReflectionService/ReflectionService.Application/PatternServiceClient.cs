using System.Net;

namespace ReflectionService.Application;

public sealed class PatternServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PatternServiceClient> _log;

    public PatternServiceClient(HttpClient http, ILogger<PatternServiceClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<string?> GetManifestAsync(long taskId, CancellationToken ct)
    {
        var url = $"/manifest?taskId={taskId}";
        using var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("PatternService manifest request failed: {StatusCode}", resp.StatusCode);
            resp.EnsureSuccessStatusCode();
        }

        return await resp.Content.ReadAsStringAsync(ct);
    }
}
