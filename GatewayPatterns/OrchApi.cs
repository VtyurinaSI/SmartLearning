internal class OrchApi
{
    private readonly HttpClient _http;
    public OrchApi(HttpClient http) => _http = http;
    public Task<HttpResponseMessage> ChatAsync(string content, CancellationToken ct) =>
         _http.PostAsJsonAsync("/workflows", content, ct);
}