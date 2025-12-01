
using SmartLearning.Contracts;

namespace GatewayPatterns
{
    public class ObjectStorageClient : IObjectStorageClient
    {
        private readonly HttpClient _http;
        public ObjectStorageClient(HttpClient http) => _http = http;

        public async Task<Guid> SaveOrigCodeAsync(RecievedForChecking origCode, Guid userId, Guid checkingId, CancellationToken ct)
        {

            var url =
                $"/objects/load/file" +
                $"?userId={userId}" +
                $"&taskId={origCode.TaskId}"+
                $"&name={"file1"}";

            var resp = await _http.PostAsync(url,
                new StringContent(origCode.OrigCode, System.Text.Encoding.UTF8, "text/plain"),
                ct);
            resp.EnsureSuccessStatusCode();
            return checkingId;
        }

        public async Task SaveReviewAsync(Guid checkingId, string review, CancellationToken ct)
        {
            var resp = await _http.PostAsync($"/objects/review?checkingId={checkingId}",
                new StringContent(review, System.Text.Encoding.UTF8, "text/plain"), ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<string?> ReadOrigCodeAsync(Guid checkingId, CancellationToken ct)
            => await _http.GetStringAsync($"/objects/orig-code/{checkingId}", ct);

        public async Task<string?> ReadReviewAsync(Guid checkingId, CancellationToken ct)
            => await _http.GetStringAsync($"/objects/review/{checkingId}", ct);
    }
}
