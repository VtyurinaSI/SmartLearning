using GatewayPatterns.Infrastructure;
using MassTransit;
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
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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
                // 1) достаём исходный код по CorrelationId
                var origCode = await _repo.ReadOrigCodeAsync(ctx.Message.CorrelationId, ctx.CancellationToken);

                // 2) собираем тот же формат запроса, что и в контроллере
                //    (messages: [{ role:"user", content: "<твой prompt + код>" }])
                var userPrompt =
                    "Сделай краткий code review. " +
                    "Обрати внимание на читаемость, тестируемость, безопасность и возможные баги. " +
                    "Ответ на русском. Код ниже:\n\n" + origCode;

                var request = new ChatRequest(
                    model: "qwen2.5-coder:14b", // тот же модельный тег, что в контроллере
                    messages: new[] { new ChatMessage("user", userPrompt) }
                );

                var reqJson = JsonSerializer.Serialize(request);
                using var content = new StringContent(reqJson, Encoding.UTF8, "application/json");

                // 3) отправляем РОВНО туда же, куда стучится контроллер: /v1/chat/completions
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

                // 4) парсим как в контроллере: choices[0].message
                ChatResponse? chat = null;
                try { chat = JsonSerializer.Deserialize<ChatResponse>(respBody); }
                catch (Exception ex) { _log.LogError(ex, "Failed to parse Ollama response for {Cid}", ctx.Message.CorrelationId); }

                var answer = chat?.choices?.FirstOrDefault()?.message?.content ?? string.Empty;

                _log.LogInformation("Ollama review (cid {Cid}): {Text}", ctx.Message.CorrelationId, answer);

                // 5) сохраняем и публикуем успешный результат
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

