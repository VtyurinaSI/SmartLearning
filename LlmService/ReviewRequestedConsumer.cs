using MassTransit;
using SmartLearning.Contracts;
using System.Text;

namespace LlmService;

public sealed class ReviewRequestedConsumer : IConsumer<ReviewRequested>
{
    private readonly ILogger<ReviewRequestedConsumer> _log;
    private readonly ReviewSourceLoader _sourceLoader;
    private readonly ReviewFileCollector _fileCollector;
    private readonly ReviewProjectStructureBuilder _structureBuilder;
    private readonly ReviewPromptBuilder _promptBuilder;
    private readonly OllamaChatClient _ollama;
    private readonly ReviewResultParser _parser;
    private readonly ReviewResponseFormatter _formatter;
    private readonly IReviewStorageClient _storage;
    private readonly WorkDirCleaner _cleaner;

    public ReviewRequestedConsumer(
        ILogger<ReviewRequestedConsumer> log,
        ReviewSourceLoader sourceLoader,
        ReviewFileCollector fileCollector,
        ReviewProjectStructureBuilder structureBuilder,
        ReviewPromptBuilder promptBuilder,
        OllamaChatClient ollama,
        ReviewResultParser parser,
        ReviewResponseFormatter formatter,
        IReviewStorageClient storage,
        WorkDirCleaner cleaner)
    {
        _log = log;
        _sourceLoader = sourceLoader;
        _fileCollector = fileCollector;
        _structureBuilder = structureBuilder;
        _promptBuilder = promptBuilder;
        _ollama = ollama;
        _parser = parser;
        _formatter = formatter;
        _storage = storage;
        _cleaner = cleaner;
    }

    public async Task Consume(ConsumeContext<ReviewRequested> context)
    {
        var msg = context.Message;
        string? workDir = null;

        try
        {
            _log.LogInformation("ReviewRequested received. UserId={UserId} TaskId={TaskId}", msg.UserId, msg.TaskId);

            var source = await _sourceLoader.LoadAsync(msg.UserId, msg.TaskId, msg.CorrelationId, context.CancellationToken);

            workDir = source.WorkDir;

            _log.LogInformation(
                "Downloaded sources. Bytes={Bytes} ContentType={ContentType} FileName={FileName} ElapsedMs={ElapsedMs}",
                source.Bytes,
                source.ContentType,
                source.FileName,
                source.DownloadTime.TotalMilliseconds);

            _log.LogInformation("Source format detected. IsZip={IsZip}", source.IsZip);

            if (source.IsZip && source.ExtractInfo is not null && source.ExtractTime is not null)
            {
                _log.LogInformation(
                    "Extracted zip. Entries={Entries} Files={Files} Dirs={Dirs} TotalUncompressedBytes={TotalUncompressedBytes} ElapsedMs={ElapsedMs}",
                    source.ExtractInfo.Entries,
                    source.ExtractInfo.Files,
                    source.ExtractInfo.Dirs,
                    source.ExtractInfo.TotalUncompressedBytes,
                    source.ExtractTime.Value.TotalMilliseconds);
            }

            var files = _fileCollector.CollectRelevantFiles(workDir);

            if (files.Count == 0)
                throw new InvalidOperationException("No relevant files found in sources (.cs/.csproj/.sln)");

            var projectStructure = _structureBuilder.Build(workDir, files);

            var patternName = string.IsNullOrWhiteSpace(msg.PatternName) ? "неизвестный" : msg.PatternName.Trim();
            var prompt = _promptBuilder.Build(
                rootDir: workDir,
                files: files,
                projectStructure: projectStructure,
                patternName: patternName);

            var chat = await _ollama.SendAsync(prompt, context.CancellationToken);

            if (!chat.IsSuccess)
            {
                _log.LogError(
                    "Ollama chat failed ({Status}) for {Cid}: {Body}",
                    chat.StatusCode,
                    msg.CorrelationId,
                    chat.ResponseBody);

                await context.Publish(new ReviewFailed(msg.CorrelationId, msg.UserId, msg.TaskId, chat.ResponseBody));
                return;
            }

            var decision = _parser.Parse(chat.Answer);
            var finalText = _formatter.Format(decision);
            var publicText = BuildPublicText(decision, includeScore: true);
            var publicFailedText = BuildPublicText(decision, includeScore: false);

            var finalBytes = Encoding.UTF8.GetBytes(finalText);

            await _storage.UploadStageAsync(
                msg.UserId,
                msg.TaskId,
                "llm",
                "review.txt",
                finalBytes,
                "text/plain",
                "utf-8",
                context.CancellationToken);

            if (decision.Passed)
                await context.Publish(new ReviewFinished(msg.CorrelationId, msg.UserId, msg.TaskId, publicText));
            else
                await context.Publish(new ReviewFailed(msg.CorrelationId, msg.UserId, msg.TaskId, publicFailedText));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Review failed for {Cid}", msg.CorrelationId);
            await context.Publish(new ReviewFailed(msg.CorrelationId, msg.UserId, msg.TaskId, ex.ToString()));
        }
        finally
        {
            _cleaner.TryDelete(workDir);
        }
    }

    private static string BuildPublicText(ReviewDecision decision, bool includeScore)
    {
        var explanation = StripEnvelope(decision.Explanation).Trim();
        if (string.IsNullOrWhiteSpace(explanation))
            explanation = StripEnvelope(decision.RawAnswer).Trim();
        if (!includeScore)
            return explanation;
        return explanation + $"\nОценка от 0 до 1: {decision.Confidence:0.00}";
    }

    private static string StripEnvelope(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var value = text;
        var rawIndex = value.IndexOf("raw_llm_response:", StringComparison.OrdinalIgnoreCase);
        if (rawIndex >= 0)
            value = value[..rawIndex];

        var explanationIndex = value.IndexOf("explanation:", StringComparison.OrdinalIgnoreCase);
        if (explanationIndex >= 0)
            value = value[(explanationIndex + "explanation:".Length)..];

        return value.Trim();
    }
}
