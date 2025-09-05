public class ProgressApi
{
    private readonly HttpClient _http;
    public ProgressApi(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> PingAsync(string userLogin, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/userid/{userLogin}");
        req.Headers.Remove("X-Echo");
        req.Headers.TryAddWithoutValidation("X-Echo", userLogin);
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}

