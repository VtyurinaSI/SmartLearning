using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gateway
{
    public class GatewayObjectStorageClient
    {
        private readonly HttpClient _http;
        public GatewayObjectStorageClient(HttpClient http) => _http = http;

        public async Task WriteFile(string data, Guid checkingId, Guid userId, long taskId, string stage, CancellationToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(data ?? string.Empty);
            using var ms = new MemoryStream(bytes);

            await WriteFileInternal(ms, $"{DateTime.Now}.txt", userId, taskId, stage, token);
        }

        public async Task WriteFile(Stream data, string fileName, Guid userId, long taskId, string stage, CancellationToken token)
        {
            await WriteFileInternal(data, fileName, userId, taskId, stage, token);
        }

        private async Task WriteFileInternal(Stream data, string fileName, Guid userId, long taskId, string stage, CancellationToken token)
        {
            var url = $"/objects/{stage}/file?userId={userId}&taskId={taskId}&fileName={fileName}";

            using var content = new StreamContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var resp = await _http.PostAsync(url, content, token);
            resp.EnsureSuccessStatusCode();
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

