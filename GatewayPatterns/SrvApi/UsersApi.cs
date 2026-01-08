
using System.Net.Http.Json;

sealed class UsersApi
{
    private readonly HttpClient _http;
    public UsersApi(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> PingAsync(string msg, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/ping");
        req.Headers.Remove("X-Echo");
        req.Headers.TryAddWithoutValidation("X-Echo", msg);
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetMeAsync(CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> UpdateMeAsync(UpdateProfileRequest dto, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me")
        {
            Content = JsonContent.Create(dto)
        };
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public Task<HttpResponseMessage> SetUserRoleAsync(Guid userId, SetUserRoleRequest dto, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}/role")
        {
            Content = JsonContent.Create(dto)
        };
        return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}


