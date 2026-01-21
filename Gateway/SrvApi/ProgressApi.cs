public class ProgressApi
{
    private readonly HttpClient _http;
    public ProgressApi(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> GetUserIdAsync(string userLogin, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/userid/{userLogin}");
        req.Headers.Remove("X-Echo");
        req.Headers.TryAddWithoutValidation("X-Echo", userLogin);
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
    public Task<HttpResponseMessage> GetUserProgressAsync(Guid userId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/user_progress/{userId}");
        req.Headers.Remove("X-Echo");
        req.Headers.TryAddWithoutValidation("X-Echo", userId.ToString());
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetTaskProgressAsync(Guid userId, long taskId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/user_progress/task/{taskId}");
        req.Headers.Remove("X-User-Id");
        req.Headers.TryAddWithoutValidation("X-User-Id", userId.ToString());
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}

