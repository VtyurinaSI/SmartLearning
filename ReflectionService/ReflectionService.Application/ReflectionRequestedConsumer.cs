using MassTransit;
using ReflectionService.Domain;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.PipelineOfCheck;
using ReflectionService.Domain.Reporting;
using SmartLearning.Contracts;
using SmartLearning.FilesUtils;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace ReflectionService.Application;

public sealed class ReflectionRequestedConsumer : IConsumer<TestRequested>
{
    private readonly ILogger<ReflectionRequestedConsumer> _log;
    private readonly HttpClient _http;
    private readonly CheckingPipeline _pipeline;
    private readonly PatternServiceClient _patterns;

    public ReflectionRequestedConsumer(
        ILogger<ReflectionRequestedConsumer> log,
        HttpClient http,
        CheckingPipeline pipeline,
        PatternServiceClient patterns,
        ICheckingReportBuilder reporter)
    {
        _log = log;
        _http = http;
        _pipeline = pipeline;
        _patterns = patterns;
        _reporter = reporter;
    }
    private readonly ICheckingReportBuilder _reporter;
    public async Task Consume(ConsumeContext<TestRequested> context)
    {
        string? workDir = null;

        _log.LogInformation(
            "ReflectionRequested: CorrelationId={Cid} UserId={Uid} TaskId={Tid}",
            context.Message.CorrelationId,
            context.Message.UserId,
            context.Message.TaskId);

        try
        {
            var build = await SourceStageLoader.LoadAsync(
                download: ct => DownloadStageAsync(
                    userId: context.Message.UserId,
                    taskId: context.Message.TaskId,
                    stage: "build",
                    fileName: null,
                    ct: ct),
                correlationId: context.Message.CorrelationId,
                ct: context.CancellationToken);

            workDir = build.WorkDir;

            _log.LogInformation(
                "Build artifacts loaded. Bytes={Bytes} ContentType={ContentType} FileName={FileName} DownloadMs={ElapsedMs}",
                build.Bytes,
                build.ContentType,
                build.FileName,
                build.DownloadTime.TotalMilliseconds);

            var entryAssemblyPath = FindEntryAssembly(workDir);

            _log.LogInformation("Entry assembly selected: {Assembly}", entryAssemblyPath);

            var manifestJson = await _patterns.GetManifestAsync(
                context.Message.TaskId,
                context.CancellationToken);

            if (string.IsNullOrWhiteSpace(manifestJson))
                throw new InvalidOperationException($"Manifest not found for taskId={context.Message.TaskId}");

            var manifest = JsonSerializer.Deserialize<CheckManifest>(
                manifestJson,
                JsonOptions.ManifestArgsConverterOptions)
                ?? throw new InvalidOperationException("Manifest deserialization failed");

            var alc = new AssemblyLoadContext(
                $"User_{context.Message.UserId}_Task_{context.Message.TaskId}",
                isCollectible: true);

            alc.Resolving += (_, name) =>
            {
                var depPath = Path.Combine(workDir, $"{name.Name}.dll");
                return File.Exists(depPath)
                    ? alc.LoadFromAssemblyPath(depPath)
                    : null;
            };
            CheckingContext? checkingCtx = null;
            try
            {
                var asm = alc.LoadFromAssemblyPath(entryAssemblyPath);

                _pipeline.SetPipeline(manifest);
                _log.LogInformation("Pipeline set: {ops}", string.Join(", ", manifest.Steps.Select(s => $"{s.Id}:{s.Operation}")));

                checkingCtx = _pipeline.ExecutePipeline(asm, manifest.Target);

                var passed = checkingCtx.StepResults.All(r => r.Passed);

                _log.LogInformation(
                    "Reflection finished. Passed={Passed} Steps={Steps}. Stat:\n{st}",
                    passed,
                    checkingCtx.StepResults.Count,
                    checkingCtx.StepResults
                    );

                string report = _reporter.Build(checkingCtx);

                if (passed)
                {
                    await context.Publish(new TestsFinished(
                        context.Message.CorrelationId,
                        context.Message.UserId,
                        context.Message.TaskId,
                        report));
                }
                else
                {
                    await context.Publish(new TestsFailed(
                        context.Message.CorrelationId,
                        context.Message.UserId,
                        context.Message.TaskId,
                        report));
                }
            }
            catch (Exception ex)
            {
                string rep = checkingCtx != null ? _reporter.Build(checkingCtx) : ex.ToString();
                await context.Publish(new TestsFailed(
                                       context.Message.CorrelationId,
                                       context.Message.UserId,
                                       context.Message.TaskId,
                                       rep));
                _log.LogError("Ошибка! \n{e}", ex);
            }
            finally
            {
                if (checkingCtx != null)
                {
                    string checkReport = _reporter.Build(checkingCtx);
                    _log.LogDebug(checkReport);
                }
                alc.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reflection failed");

            await context.Publish(new TestsFailed(
                context.Message.CorrelationId,
                context.Message.UserId,
                context.Message.TaskId,
                ex.ToString()));
        }
        finally
        {
            if (workDir is not null)
                TryDelete(workDir);
        }
    }

    private async Task<SourceStageLoader.StorageDownload> DownloadStageAsync(
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
        var name = resp.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "build.zip";
        var ctType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new SourceStageLoader.StorageDownload(bytes, ctType, name);
    }

    private static string FindEntryAssembly(string root)
    {
        foreach (var dll in Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                AssemblyName.GetAssemblyName(dll);
                return dll;
            }
            catch (BadImageFormatException)
            {
            }
        }

        throw new InvalidOperationException("No managed entry assembly found in build artifacts");
    }


    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }
}
