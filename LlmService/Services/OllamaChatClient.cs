using System.Text;
using System.Text.Json;

namespace LlmService;

public sealed class OllamaChatClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaChatClient> _log;

    public OllamaChatClient(IHttpClientFactory factory, ILogger<OllamaChatClient> log)
    {
        _http = factory.CreateClient("Ollama");
        _log = log;
    }

    public async Task<OllamaChatResult> SendAsync(string prompt, CancellationToken ct)
    {
        var request = new ChatRequest(
            model: GetEnv("OLLAMA_MODEL", "llama3.1"),
            messages: new[] { new ChatMessage("user", prompt) });

        var reqJson = JsonSerializer.Serialize(request);
        using var reqContent = new StringContent(reqJson, Encoding.UTF8, "application/json");

        using var resp = await _http.PostAsync("v1/chat/completions", reqContent, ct);
        var respBody = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            return new OllamaChatResult(false, (int)resp.StatusCode, respBody, string.Empty);
        }

        ChatResponse? chat = null;
        try { chat = JsonSerializer.Deserialize<ChatResponse>(respBody); }
        catch (Exception ex) { _log.LogError(ex, "Failed to parse Ollama response"); }

        var answer = chat?.choices?.FirstOrDefault()?.message?.content ?? string.Empty;
        return new OllamaChatResult(true, (int)resp.StatusCode, respBody, answer);
    }

    private static string GetEnv(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;
}

public sealed record OllamaChatResult(bool IsSuccess, int StatusCode, string ResponseBody, string Answer);
