using MassTransit;
using Microsoft.Extensions.Options;
using SmartLearning.Contracts;
using System.Text;

namespace OrchestrPatterns.Application
{
    public sealed class CheckRequestHandler
    {
        private readonly IBus _bus;
        private readonly CompletionHub _hub;
        private readonly IHttpClientFactory _http;
        private readonly PatternServiceClient _patterns;
        private readonly ILogger<CheckRequestHandler> _log;
        private readonly CheckTimeoutOptions _timeouts;
        private readonly ReviewStorageOptions _reviewStorage;

        public CheckRequestHandler(
            IBus bus,
            CompletionHub hub,
            IHttpClientFactory http,
            PatternServiceClient patterns,
            ILogger<CheckRequestHandler> log,
            IOptions<CheckTimeoutOptions> timeouts,
            IOptions<ReviewStorageOptions> reviewStorage)
        {
            _bus = bus;
            _hub = hub;
            _http = http;
            _patterns = patterns;
            _log = log;
            _timeouts = timeouts.Value;
            _reviewStorage = reviewStorage.Value;
        }

        public async Task<IResult> HandleAsync(StartChecking dto, CancellationToken ct)
        {
            var id = dto.CorrelationId == Guid.Empty ? NewId.NextGuid() : dto.CorrelationId;

            var exists = await _patterns.TaskExistsAsync(dto.TaskId, ct);
            if (exists == false)
                return Results.NotFound($"-ø?ø‘Øø ‘? id {dto.TaskId} ?ç ?øü?ç?ø.");
            if (exists is null)
                _log.LogWarning("PatternService unavailable while checking task {TaskId}", dto.TaskId);

            var taskName = await _patterns.GetTaskTitleAsync(dto.TaskId, ct);
            if (string.IsNullOrWhiteSpace(taskName))
                taskName = $"task {dto.TaskId}";

            await _bus.Publish(new CompileRequested(id, dto.UserId, dto.TaskId), ct);

            var (okCompile, compilRes) = await _hub.WaitAsync(id, TimeSpan.FromMinutes(_timeouts.CompileMinutes), ct);
            await Task.Delay(_timeouts.PostCompileDelayMs, ct);

            if (!okCompile)
            {
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, false, false, false, id, false, compilRes, null, null), ct);
                var progressFailCompile = new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    false,
                    false, compilRes,
                    false, null,
                    false, null);
                return Results.Ok(progressFailCompile);
            }

            await _bus.Publish(new TestRequested(id, dto.UserId, dto.TaskId), ct);

            var (okReflection, testRes) = await _hub.WaitAsync(id, TimeSpan.FromMinutes(_timeouts.TestMinutes), ct);

            if (!okReflection)
            {
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, false, false, id, false, compilRes, testRes, null), ct);
                return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    false,
                    true, compilRes,
                    false, testRes,
                    false, null));
            }

            var patternName = await _patterns.GetPatternTitleAsync(dto.TaskId, ct);
            if (string.IsNullOrWhiteSpace(patternName))
                patternName = string.Empty;
            await _bus.Publish(new ReviewRequested(id, dto.UserId, dto.TaskId, patternName), ct);
            var (okReview, reviewRes) = await _hub.WaitAsync(id, TimeSpan.FromMinutes(_timeouts.ReviewMinutes), ct);

            if (!okReview)
            {
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, true, false, id, false, compilRes, testRes, reviewRes), ct);
                return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    false,
                    true, compilRes,
                    true, testRes,
                    false, reviewRes));
            }

            var minioClient = _http.CreateClient("MinioStorage");
            var url = $"/objects/llm/file?userId={dto.UserId}&taskId={dto.TaskId}&fileName={_reviewStorage.FileName}";

            try
            {
                using var respMinio = await minioClient.GetAsync(url, ct);
                if (respMinio.IsSuccessStatusCode)
                {
                    var bytes = await respMinio.Content.ReadAsByteArrayAsync(ct);
                    reviewRes = Encoding.UTF8.GetString(bytes);
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Failed to read review from storage for {Cid}", id);
            }

            await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, true, true, id, true, compilRes, testRes, reviewRes), ct);
            return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                true,
                true, compilRes,
                true, testRes,
                true, reviewRes));
        }
    }
}
