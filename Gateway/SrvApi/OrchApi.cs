using SmartLearning.Contracts;

internal class OrchApi
{
    private readonly HttpClient _http;
    public OrchApi(HttpClient http) => _http = http;
    public Task<HttpResponseMessage> StartCheckAsync(StartChecking content, CancellationToken ct)
    {
        return _http.PostAsJsonAsync("/orc/check", content, ct);
    }
    
}