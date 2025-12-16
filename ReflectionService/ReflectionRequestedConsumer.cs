using MassTransit;
using SmartLearning.Contracts;
using System.Net;
using System.Text;

namespace ReflectionService
{
    public class ReflectionRequestedConsumer : IConsumer<TestRequested>
    {
        private readonly ILogger<ReflectionRequestedConsumer> _log;
        //private readonly IObjectStorageRepository _repo;
        private readonly HttpClient _http;

        public ReflectionRequestedConsumer(/*IObjectStorageRepository repo,*/ ILogger<ReflectionRequestedConsumer> log, HttpClient http)
        {
            //_repo = repo;
            _log = log;
            _http = http;
        }
        private async Task<byte[]> LoadSourceAsync(ConsumeContext<TestRequested> context)
        {
            _log.LogInformation("Запрос на рефлексию!");
            var url = $"/objects/build/file?userId={context.Message.UserId}&taskId={context.Message.TaskId}&fileName={"program.dll"}";

            using var resp = await _http.GetAsync(url, context.CancellationToken);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                _log.LogError("ObjectStorage returned 404 for {Cid}", context.Message.CorrelationId);
                return Array.Empty<byte>();
            }

            resp.EnsureSuccessStatusCode();

            _log.LogInformation("Сборка успешно прочитан!");
            return await resp.Content.ReadAsByteArrayAsync(context.CancellationToken);
        }
        public async Task Consume(ConsumeContext<TestRequested> context)
        {
            _log.LogInformation("Reflection requested: CorrelationId={Cid}, UserId={UserId}, TaskId={TaskId}",
                context.Message.CorrelationId, context.Message.UserId, context.Message.TaskId);
            var origCode = await LoadSourceAsync(context);

            await context.Publish(new TestsFinished(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
            _log.LogInformation("ЗАВЕРШЕНИЕ...");

        }
    }
}
