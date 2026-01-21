using MassTransit;
using Microsoft.Extensions.Options;
using SmartLearning.Contracts;

namespace Orchestrator.Application.Services
{
    public sealed class CheckRequestHandler
    {
        private readonly IBus _bus;
        private readonly CompletionHub _hub;
        private readonly PatternServiceClient _patterns;
        private readonly ILogger<CheckRequestHandler> _log;
        private readonly CheckTimeoutOptions _timeouts;

        public CheckRequestHandler(
            IBus bus,
            CompletionHub hub,
            PatternServiceClient patterns,
            ILogger<CheckRequestHandler> log,
            IOptions<CheckTimeoutOptions> timeouts)
        {
            _bus = bus;
            _hub = hub;
            _patterns = patterns;
            _log = log;
            _timeouts = timeouts.Value;
        }

        public async Task<IResult> HandleAsync(StartCheckRequest dto, CancellationToken ct)
        {
            var exists = await _patterns.TaskExistsAsync(dto.TaskId, ct);
            if (exists == false)
                return Results.NotFound($"Задание с id {dto.TaskId} не найдено.");
            if (exists is null)
                _log.LogWarning("PatternService unavailable while checking task {TaskId}", dto.TaskId);

            var taskName = await _patterns.GetTaskTitleAsync(dto.TaskId, ct);
            if (string.IsNullOrWhiteSpace(taskName))
                taskName = $"task {dto.TaskId}";

            var correlationId = NewId.NextGuid();
            var start = new StartChecking(correlationId, dto.UserId, dto.TaskId, taskName);
            await _bus.Publish(start, ct);
            /*await _bus.Publish(new CompileRequested(id, dto.UserId, dto.TaskId), ct);

            var (okCompile, compilRes) = await _hub.WaitAsync(id, TimeSpan.FromMinutes(_timeouts.CompileMinutes), ct);
            await Task.Delay(_timeouts.PostCompileDelayMs, ct);

            if (!okCompile)
            {
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, false, false, false, id, true, false, compilRes, null, null), ct);
                var progressFailCompile = new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    true,
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
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, false, false, id, true, false, compilRes, testRes, null), ct);
                return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    true,
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
                await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, true, false, id, true, false, compilRes, testRes, reviewRes), ct);
                return Results.Ok(new UserProgressRow(dto.UserId, dto.TaskId, taskName, id,
                    true,
                    false,
                    true, compilRes,
                    true, testRes,
                    false, reviewRes));
            }

            await _bus.Publish(new UpdateProgress(dto.UserId, dto.TaskId, taskName, true, true, true, id, true, true, compilRes, testRes, reviewRes), ct);
            */
            return Results.Accepted(/*$"/orc/check/{correlationId}", new { correlationId }*/);
        }
    }
}


