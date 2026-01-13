using SmartLearning.FilesUtils;
using System.Net;
using System.Net.Http.Headers;

namespace CompilerService;

public sealed class StageStorageClient : IStageStorageClient
{
    private readonly HttpClient _http;

    public StageStorageClient(HttpClient http)
    {
        _http = http;
    }

    public async Task UploadStageAsync(
        Guid userId,
        long taskId,
        string stage,
        string fileName,
        byte[] bytes,
        string contentType,
        CancellationToken ct)
    {
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var url = $"/objects/{stage}/file?userId={userId}&taskId={taskId}&fileName={Uri.EscapeDataString(fileName)}";
        using var resp = await _http.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<SourceStageLoader.StorageDownload> DownloadStageAsync(
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
            return new SourceStageLoader.StorageDownload(Array.Empty<byte>(), "", "");

        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var name = resp.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "source.zip";
        var ctType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new SourceStageLoader.StorageDownload(bytes, ctType, name);
    }
}

