using System.Net;

namespace PatternService;

public sealed class ContentStorage(HttpClient http) : IContentStorage
{
    private readonly HttpClient _http = http;

    public async Task<byte[]?> GetAsync(string key, CancellationToken ct)
    {
        var url = $"/patterns/file?key={Uri.EscapeDataString(key)}";
        using var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }
}

public interface IContentStorage
{
    Task<byte[]?> GetAsync(string key, CancellationToken ct);
}
