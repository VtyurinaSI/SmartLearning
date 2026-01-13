using CompilerService.Services;
using MassTransit;
using SmartLearning.Contracts;
using SmartLearning.FilesUtils;
using System.Diagnostics;

namespace CompilerService;

public sealed class CompileRequestedConsumer : IConsumer<CompileRequested>
{
    private readonly ILogger<CompileRequestedConsumer> _log;
    private readonly SourceLoadService _sourceLoader;
    private readonly BuildTargetLocator _targetLocator;
    private readonly DotnetRunner _dotnet;
    private readonly BuildOutputUploader _uploader;
    private readonly WorkDirCleaner _cleaner;

    public CompileRequestedConsumer(
        ILogger<CompileRequestedConsumer> log,
        SourceLoadService sourceLoader,
        BuildTargetLocator targetLocator,
        DotnetRunner dotnet,
        BuildOutputUploader uploader,
        WorkDirCleaner cleaner)
    {
        _log = log;
        _sourceLoader = sourceLoader;
        _targetLocator = targetLocator;
        _dotnet = dotnet;
        _uploader = uploader;
        _cleaner = cleaner;
    }

    public async Task Consume(ConsumeContext<CompileRequested> context)
    {
        var msg = context.Message;

        string? workDir = null;

        try
        {
            _log.LogInformation("CompileRequested received. UserId={UserId} TaskId={TaskId}", msg.UserId, msg.TaskId);

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

            var extractDir = workDir;

            var targets = _targetLocator.FindTargets(extractDir);

            if (targets.Solutions.Count == 0 && targets.Projects.Count == 0)
                throw new InvalidOperationException("No solution or project files found");

            if (targets.Solutions.Count > 0)
                _log.LogInformation("Solutions: {List}", string.Join(" | ", targets.Solutions.Take(20)));

            if (targets.Projects.Count > 0)
                _log.LogInformation("Projects: {List}", string.Join(" | ", targets.Projects.Take(20)));

            var target = _targetLocator.FindBuildTarget(extractDir);
            var targetDir = Path.GetDirectoryName(target)!;

            _log.LogInformation("Selected build target: {Target}. TargetDir={TargetDir}", target, targetDir);

            var outDir = Path.Combine(workDir, "_out");
            Directory.CreateDirectory(outDir);

            var swRestore = Stopwatch.StartNew();
            var restore = await _dotnet.RunAsync(targetDir, $"restore \"{target}\" --nologo", context.CancellationToken);
            swRestore.Stop();

            _log.LogInformation(
                "dotnet restore finished in {Elapsed}ms ExitCode={ExitCode}",
                swRestore.ElapsedMilliseconds,
                restore.ExitCode);

            if (restore.ExitCode != 0)
                throw new InvalidOperationException($"dotnet restore failed: {Trim(restore.StdErr, 4000)}");

            var swBuild = Stopwatch.StartNew();
            var build = await _dotnet.RunAsync(
                targetDir,
                $"build \"{target}\" -c Release -o \"{outDir}\" --nologo",
                context.CancellationToken);
            swBuild.Stop();

            _log.LogInformation(
                "dotnet build finished in {Elapsed}ms ExitCode={ExitCode}",
                swBuild.ElapsedMilliseconds,
                build.ExitCode);

            var buildLog = build.StdOut + "\n" + build.StdErr;

            if (build.ExitCode != 0)
            {
                _log.LogError("Build failed: {err}", Trim(build.StdErr, 4000));

                await context.Publish(new CompilationFailed(
                    msg.CorrelationId,
                    msg.UserId,
                    msg.TaskId,
                    Trim(buildLog, 20000)));
                return;
            }

            var files = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                _log.LogError("Build produced no output files");
                await context.Publish(new CompilationFailed(
                    msg.CorrelationId,
                    msg.UserId,
                    msg.TaskId,
                    "Build produced no output files"));
                return;
            }

            await _uploader.UploadBuildOutputAsync(msg.UserId, msg.TaskId, outDir, context.CancellationToken);

            await context.Publish(new CompilationFinished(
                msg.CorrelationId,
                msg.UserId,
                msg.TaskId,
                Trim(buildLog, 20000)));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Compile failed");

            await context.Publish(new CompilationFailed(
                msg.CorrelationId,
                msg.UserId,
                msg.TaskId,
                ex.ToString()));
        }
        finally
        {
            _cleaner.TryDelete(workDir);
        }
    }

    private static string Trim(string s, int max)
        => s.Length <= max ? s : s[..max];
}

