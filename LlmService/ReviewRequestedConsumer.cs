using MassTransit;
using SmartLearning.Contracts;
using SmartLearning.FilesUtils;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LlmService;

public sealed class ReviewRequestedConsumer : IConsumer<ReviewRequested>
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ReviewRequestedConsumer> _log;

    public ReviewRequestedConsumer(
        IHttpClientFactory http,
        ILogger<ReviewRequestedConsumer> log)
    {
        _http = http;
        _log = log;
    }

    private sealed record StorageDownload(byte[] Bytes, string ContentType, string FileName);

    public sealed record ChatMessage(string role, string content);
    public sealed record ChatRequest(string model, ChatMessage[] messages);
    public sealed record Choice(int index, ChatMessage message);
    public sealed record ChatResponse(Choice[] choices);

    public async Task Consume(ConsumeContext<ReviewRequested> context)
    {
        var msg = context.Message;
        string? workDir = null;

        try
        {
            _log.LogInformation("ReviewRequested received. UserId={UserId} TaskId={TaskId}", msg.UserId, msg.TaskId);

            var storage = _http.CreateClient("MinioStorage");

            var dl = await DownloadStageAsync(
                storage,
                userId: msg.UserId,
                taskId: msg.TaskId,
                stage: "load",
                fileName: null,
                ct: context.CancellationToken);

            if (dl.Bytes.Length == 0)
                throw new InvalidOperationException("No sources found in load stage");

            workDir = CreateWorkDir(msg.CorrelationId);
            RecreateDir(workDir);

            var isZip =
                IsZip(dl.Bytes) ||
                dl.ContentType.Contains("zip", StringComparison.OrdinalIgnoreCase) ||
                dl.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

            if (isZip)
            {
                ArchiveTools.ExtractZipToDirectory(dl.Bytes, workDir);
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(dl.FileName) ? "source.txt" : dl.FileName;
                var safe = SanitizeFileName(name);
                var path = Path.Combine(workDir, safe);
                await File.WriteAllBytesAsync(path, dl.Bytes, context.CancellationToken);
            }

            var files = CollectRelevantFiles(workDir);

            if (files.Count == 0)
                throw new InvalidOperationException("No relevant files found in sources (.cs/.csproj/.sln)");

            var projectStructure = BuildProjectStructure(workDir, files);

            var prompt = BuildPrompt(
                rootDir: workDir,
                files: files,
                projectStructure: projectStructure,
                maxChars: GetEnvInt("LLM_PROMPT_MAX_CHARS", 120_000),
                perFileMaxChars: GetEnvInt("LLM_PROMPT_PER_FILE_MAX_CHARS", 20_000));

            var request = new ChatRequest(
                model: GetEnv("OLLAMA_MODEL", "llama3.1"),
                messages: new[] { new ChatMessage("user", prompt) });

            var reqJson = JsonSerializer.Serialize(request);
            using var reqContent = new StringContent(reqJson, Encoding.UTF8, "application/json");

            var ollama = _http.CreateClient("Ollama");
            using var resp = await ollama.PostAsync("v1/chat/completions", reqContent, context.CancellationToken);
            var respBody = await resp.Content.ReadAsStringAsync(context.CancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogError(
                    "Ollama chat failed ({Status}) for {Cid}: {Body}",
                    (int)resp.StatusCode,
                    msg.CorrelationId,
                    respBody);

                await context.Publish(new ReviewFailed(msg.CorrelationId, msg.UserId, msg.TaskId, respBody));
                return;
            }

            ChatResponse? chat = null;
            try { chat = JsonSerializer.Deserialize<ChatResponse>(respBody); }
            catch (Exception ex) { _log.LogError(ex, "Failed to parse Ollama response for {Cid}", msg.CorrelationId); }

            var answer = chat?.choices?.FirstOrDefault()?.message?.content ?? string.Empty;

            await UploadStageAsync(
                storage,
                userId: msg.UserId,
                taskId: msg.TaskId,
                stage: "llm",
                fileName: "review.txt",
                bytes: Encoding.UTF8.GetBytes(answer),
                mediaType: "text/plain",
                charset: "utf-8",
                ct: context.CancellationToken);


            await context.Publish(new ReviewFinished(msg.CorrelationId, msg.UserId, msg.TaskId, answer));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Review failed for {Cid}", msg.CorrelationId);
            await context.Publish(new ReviewFailed(msg.CorrelationId, msg.UserId, msg.TaskId, ex.ToString()));
        }
        finally
        {
            if (workDir is not null)
                TryDelete(workDir);
        }
    }

    private static string CreateWorkDir(Guid correlationId)
    {
        var root = GetEnv("WORK_ROOT", "");
        if (string.IsNullOrWhiteSpace(root))
            root = Path.GetTempPath();

        return Path.Combine(root, "smartlearning", "review", correlationId.ToString("N"));
    }

    private static List<string> CollectRelevantFiles(string rootDir)
    {
        static bool IsExcluded(string path)
        {
            var p = path.Replace('\\', '/');
            return p.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.git/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.vs/", StringComparison.OrdinalIgnoreCase) ||
                   p.Contains("/.idea/", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsRelevant(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)) return true;
            if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) return true;

            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return false;
                if (name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }

            return false;
        }

        var all = Directory
            .EnumerateFiles(rootDir, "*", SearchOption.AllDirectories)
            .Where(p => !IsExcluded(p))
            .Where(IsRelevant)
            .ToList();

        int Priority(string p)
        {
            var name = Path.GetFileName(p);
            var ext = Path.GetExtension(p).ToLowerInvariant();

            if (ext == ".sln") return 0;
            if (ext == ".csproj") return 1;
            if (ext == ".cs" && name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)) return 3;
            if (ext == ".cs") return 2;
            return 9;
        }

        return all
            .OrderBy(Priority)
            .ThenBy(p => Path.GetRelativePath(rootDir, p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildProjectStructure(string rootDir, IReadOnlyList<string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Project structure:");
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(rootDir, f).Replace('\\', '/');
            sb.Append("- ").AppendLine(rel);
        }
        return sb.ToString();
    }

    private static string BuildPrompt(
        string rootDir,
        IReadOnlyList<string> files,
        string projectStructure,
        int maxChars,
        int perFileMaxChars)
    {
        var sb = new StringBuilder(Math.Min(maxChars, 64_000));

        sb.AppendLine("""
Ты эксперт по C#.
Сделай code review присланного решения.
Фокус: читаемость, структура, архитектурные запахи, явные баги.
Ответ: кратко, по делу, на русском. Исправленный код не пиши.
""");

        sb.AppendLine(projectStructure);
        sb.AppendLine();

        foreach (var f in files)
        {
            if (sb.Length >= maxChars)
                break;

            var rel = Path.GetRelativePath(rootDir, f).Replace('\\', '/');
            sb.AppendLine($"--- FILE: {rel} ---");

            string text;
            try { text = File.ReadAllText(f, Encoding.UTF8); }
            catch { text = File.ReadAllText(f); }

            if (text.Length > perFileMaxChars)
                text = text[..perFileMaxChars] + "\n\n[TRUNCATED]\n";

            var reserve = 256;

            if (sb.Length + text.Length + reserve > maxChars)
            {
                var space = Math.Max(0, maxChars - sb.Length - reserve);
                if (space > 0)
                    sb.AppendLine(text[..Math.Min(space, text.Length)]);
                sb.AppendLine("[TRUNCATED BY TOTAL LIMIT]");
                break;
            }

            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static async Task UploadStageAsync(
        HttpClient http,
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
        using var resp = await http.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
    }

    private static async Task<StorageDownload> DownloadStageAsync(
        HttpClient http,
        Guid userId,
        long taskId,
        string stage,
        string? fileName,
        CancellationToken ct)
    {
        var url = fileName is null
            ? $"/objects/{stage}/file?userId={userId}&taskId={taskId}"
            : $"/objects/{stage}/file?userId={userId}&taskId={taskId}&fileName={Uri.EscapeDataString(fileName)}";

        using var resp = await http.GetAsync(url, ct);

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

    private static bool IsZip(byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B;

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static void RecreateDir(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch { }
    }

    private static string GetEnv(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    private static int GetEnvInt(string key, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : fallback;
}
