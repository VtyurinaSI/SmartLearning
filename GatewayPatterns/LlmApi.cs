sealed class LlmApi
{
    private readonly HttpClient _http;
    public LlmApi(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> ChatAsync(ChatMessage[] messages, CancellationToken ct) =>
        _http.PostAsJsonAsync("api/llm/chat", messages, ct);

    public Task<HttpResponseMessage> ChatAsync(string content, CancellationToken ct) =>
        ChatAsync(new[] { new ChatMessage("user", content) }, ct);
}
