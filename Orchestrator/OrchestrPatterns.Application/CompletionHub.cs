using System.Collections.Concurrent;

namespace OrchestrPatterns.Application
{
    public class CompletionHub
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _waiters = new();

        public Task<bool> WaitAsync(Guid id, TimeSpan timeout, CancellationToken ct)
        {
            var tcs = _waiters.GetOrAdd(id, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            cts.Token.Register(() => tcs.TrySetResult(false));
            return tcs.Task;
        }

        public void SetCompleted(Guid id, bool ok)
        {
            if (_waiters.TryRemove(id, out var tcs))
                tcs.TrySetResult(ok);
        }
    }
}
