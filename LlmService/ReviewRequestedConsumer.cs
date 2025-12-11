using MassTransit;
using MinIoStub;
using SmartLearning.Contracts;
using System.Net;
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
        
        public async Task Consume(ConsumeContext<ReviewRequested> context)
        {
            try
            {
                var minioClient = _http.CreateClient("MinioStorage");
                var url = $"/objects/load/file?userId={context.Message.UserId}&taskId={context.Message.TaskId}";

                using var respMinio = await minioClient.GetAsync(url, context.CancellationToken);
                if (respMinio.StatusCode == HttpStatusCode.NotFound)
                {
                    _log.LogError("ObjectStorage returned 404 for {Cid}", context.Message.CorrelationId);
                    return;
                }

                respMinio.EnsureSuccessStatusCode();

                var bytes = await respMinio.Content.ReadAsByteArrayAsync(context.CancellationToken);
                var origCode = Encoding.UTF8.GetString(bytes);



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

                var ollamaClient = _http.CreateClient("Ollama");
                using var resp = await ollamaClient.PostAsync("v1/chat/completions", content, context.CancellationToken);

                var respBody = await resp.Content.ReadAsStringAsync(context.CancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogError("Ollama chat failed ({Status}) for {Cid}: {Body}",
                        (int)resp.StatusCode, context.Message.CorrelationId, respBody);

                    await context.Publish(new ReviewFailed(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
                    return;
                }
                ChatResponse? chat = null;
                try { chat = JsonSerializer.Deserialize<ChatResponse>(respBody); }
                catch (Exception ex) { _log.LogError(ex, "Failed to parse Ollama response for {Cid}", context.Message.CorrelationId); }

                var answer = chat?.choices?.FirstOrDefault()?.message?.content ?? string.Empty;

                _log.LogInformation("Ollama review (cid {Cid}): {Text}", context.Message.CorrelationId, answer);

                await _repo.SaveReviewAsync(context.Message.CorrelationId, answer, context.CancellationToken);
                await context.Publish(new ReviewFinished(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Review failed for {Cid}", context.Message.CorrelationId);
                await context.Publish(new ReviewFailed(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
            }
        }
    }
}

