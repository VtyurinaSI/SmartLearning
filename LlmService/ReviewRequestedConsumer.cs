using MassTransit;
using MinIoStub;
using SmartLearning.Contracts;
using System.Text;
using System.Text.Json;

namespace LlmService
{
    public class ReviewRequestedConsumer : IConsumer<ReviewRequested>
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ReviewRequestedConsumer> _log;
        private readonly IObjectStorageRepository _repo;

        public ReviewRequestedConsumer(IHttpClientFactory http, IObjectStorageRepository repo, ILogger<ReviewRequestedConsumer> log)
        {
            _http = http;
            _repo = repo;
            _log = log;
        }

        public record ChatMessage(string role, string content);
        public record ChatRequest(string model, ChatMessage[] messages);
        public record Choice(int index, ChatMessage message);
        public record ChatResponse(Choice[] choices);

        public async Task Consume(ConsumeContext<ReviewRequested> ctx)
        {
            try
            {
                var origCode = await _repo.ReadOrigCodeAsync(ctx.Message.CorrelationId, ctx.CancellationToken);

                var userPrompt = """
                    Привет! Ты эксперт в области разработки на C#.
                    Сделай code review. Обрати внимание на читаемость и возможные баги. 
                    Краткий ответ на русском. Исправленный код писать не надо.
                    Вот сам код: 
                    """ + origCode;

                var request = new ChatRequest(
                    model: "llama3.1",//"qwen2.5-coder:7b",
                    messages: new[] { new ChatMessage("user", userPrompt) }
                );

                var reqJson = JsonSerializer.Serialize(request);
                using var content = new StringContent(reqJson, Encoding.UTF8, "application/json");

                var client = _http.CreateClient("Ollama");
                using var resp = await client.PostAsync("v1/chat/completions", content, ctx.CancellationToken);

                var respBody = await resp.Content.ReadAsStringAsync(ctx.CancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogError("Ollama chat failed ({Status}) for {Cid}: {Body}",
                        (int)resp.StatusCode, ctx.Message.CorrelationId, respBody);

                    await ctx.Publish(new ReviewFailed(ctx.Message.CorrelationId));
                    return;
                }
                ChatResponse? chat = null;
                try { chat = JsonSerializer.Deserialize<ChatResponse>(respBody); }
                catch (Exception ex) { _log.LogError(ex, "Failed to parse Ollama response for {Cid}", ctx.Message.CorrelationId); }

                var answer = chat?.choices?.FirstOrDefault()?.message?.content ?? string.Empty;

                _log.LogInformation("Ollama review (cid {Cid}): {Text}", ctx.Message.CorrelationId, answer);

                await _repo.SaveReviewAsync(ctx.Message.CorrelationId, answer, ctx.CancellationToken);
                await ctx.Publish(new ReviewFinished(ctx.Message.CorrelationId));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Review failed for {Cid}", ctx.Message.CorrelationId);
                await ctx.Publish(new ReviewFailed(ctx.Message.CorrelationId));
            }
        }
    }
}

