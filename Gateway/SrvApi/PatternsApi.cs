
sealed class PatternsApi
{
    private readonly HttpClient _http;
    public PatternsApi(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> PingAsync(string msg, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/ping");
        req.Headers.Remove("X-Echo");
        req.Headers.TryAddWithoutValidation("X-Echo", msg);
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetTasksAsync(CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/tasks");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetTheoryAsync(long taskId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/theory?taskId={taskId}");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetMetaAsync(long taskId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/meta?taskId={taskId}");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetTaskTitleAsync(long taskId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/task_title?taskId={taskId}");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetTaskAsync(long taskId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/task?taskId={taskId}");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}

