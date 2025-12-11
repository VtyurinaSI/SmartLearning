using System.Text;
using System.Text.Json;

namespace GatewayPatterns
{
    public class GatewayObjectStorageClient
    {
        private readonly HttpClient _http;
        public GatewayObjectStorageClient(HttpClient http) => _http = http;

        public async Task WriteFile(string data, Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token)
        {
            var url = $"/objects/{stage}/file?userId={userId}&taskId={TaskId}&fileName={"file.txt"}";
            using var content = new StringContent(data ?? string.Empty, Encoding.UTF8, "text/plain");

            using var resp = await _http.PostAsync(url, content, token);

            if (!resp.IsSuccessStatusCode)
            {
                var respBody = await resp.Content.ReadAsStringAsync(token);
                throw new HttpRequestException(
                    $"ObjectStorage returned {(int)resp.StatusCode} {resp.ReasonPhrase}. " +
                    $"Url: {url}. Body: {respBody}");
            }
        }

        public async Task<T> ReadFile<T>(Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token)
        {
            var url = $"/objects/{stage}/file?userId={userId}&taskId={TaskId}";

            if (typeof(T) == typeof(string))
            {
                var s = await _http.GetStringAsync(url, token);
                return (T)(object)s;
            }

            if (typeof(T) == typeof(byte[]))
            {
                var b = await _http.GetByteArrayAsync(url, token);
                return (T)(object)b;
            }

            using var resp = await _http.GetAsync(url, token);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(token);
            var obj = JsonSerializer.Deserialize<T>(body);
            return obj ?? default!;
        }
    }
}
