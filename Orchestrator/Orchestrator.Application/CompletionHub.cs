using System.Collections.Concurrent;

namespace Orchestrator.Application
{
    public class CompletionHub
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<(bool Ok, string? Result)>> _waiters = new();

        public Task<(bool Ok, string? Result)> WaitAsync(Guid id, TimeSpan timeout, CancellationToken ct)
        {
            var tcs = _waiters.GetOrAdd(id, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            cts.Token.Register(() => tcs.TrySetResult((false, null)));
            return tcs.Task;
        }

        public void SetCompleted(Guid id, bool ok, string? result)
        {
            if (_waiters.TryRemove(id, out var tcs))
                tcs.TrySetResult((ok, result));
        }
    }
}

