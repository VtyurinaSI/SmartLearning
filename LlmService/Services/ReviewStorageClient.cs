using System.Net;
using System.Net.Http.Headers;

namespace LlmService;

public sealed class ReviewStorageClient : IReviewStorageClient
{
    private readonly HttpClient _http;

    public ReviewStorageClient(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient("MinioStorage");
    }

    public async Task UploadStageAsync(
        Guid userId,
        long taskId,
        string stage,
        string fileName,
        byte[] bytes,
        string mediaType,
        string? charset,
        CancellationToken ct)
    {
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        if (!string.IsNullOrWhiteSpace(charset))
            content.Headers.ContentType.CharSet = charset;

        var url = $"/objects/{stage}/file?userId={userId}&taskId={taskId}&fileName={Uri.EscapeDataString(fileName)}";
        using var resp = await _http.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<StorageDownload> DownloadStageAsync(
        Guid userId,
        long taskId,
        string stage,
        string? fileName,
        CancellationToken ct)
    {
        var url = fileName is null
            ? $"/objects/{stage}/file?userId={userId}&taskId={taskId}"
            : $"/objects/{stage}/file?userId={userId}&taskId={taskId}&fileName={Uri.EscapeDataString(fileName)}";

        using var resp = await _http.GetAsync(url, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return new StorageDownload(Array.Empty<byte>(), "", "");

        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var name =
            resp.Content.Headers.ContentDisposition?.FileNameStar?.Trim('"') ??
            resp.Content.Headers.ContentDisposition?.FileName?.Trim('"') ??
            "source.zip";
        var ctType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new StorageDownload(bytes, ctType, name);
    }
}
