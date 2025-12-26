using MassTransit;
using ReflectionService.Domain;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.PipelineOfCheck;
using SmartLearning.Contracts;
using SmartLearning.FilesUtils;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace ReflectionService;

public sealed class ReflectionRequestedConsumer : IConsumer<TestRequested>
{
    private readonly ILogger<ReflectionRequestedConsumer> _log;
    private readonly HttpClient _http;
    private readonly CheckingPipeline _pipeline;

    public ReflectionRequestedConsumer(
        ILogger<ReflectionRequestedConsumer> log,
        HttpClient http,
        CheckingPipeline pipeline)
    {
        _log = log;
        _http = http;
        _pipeline = pipeline;
    }

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

            var manifest = JsonSerializer.Deserialize<CheckManifest>(
                StrategyManifestExample.Manifest,
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

            try
            {
                var asm = alc.LoadFromAssemblyPath(entryAssemblyPath);

                _pipeline.SetPipeline(manifest);
                var checkingCtx = _pipeline.ExecutePipeline(asm, manifest.Target);

                var passed = checkingCtx.StepResults.All(r => r.Passed);

                _log.LogInformation(
                    "Reflection finished. Passed={Passed} Steps={Steps}",
                    passed,
                    checkingCtx.StepResults.Count);

                if (passed)
                {
                    await context.Publish(new TestsFinished(
                        context.Message.CorrelationId,
                        context.Message.UserId,
                        context.Message.TaskId));
                }
                else
                {
                    await context.Publish(new TestsFailed(
                        context.Message.CorrelationId,
                        context.Message.UserId,
                        context.Message.TaskId));
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new TestsFailed(
                                       context.Message.CorrelationId,
                                       context.Message.UserId,
                                       context.Message.TaskId));
                _log.LogError("Ошибка! \n{e}", ex);
            }
            finally
            {
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
                context.Message.TaskId));
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
