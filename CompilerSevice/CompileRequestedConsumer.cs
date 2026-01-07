using MassTransit;
using SmartLearning.Contracts;
using SmartLearning.FilesUtils;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace CompilerSevice;

public sealed class CompileRequestedConsumer : IConsumer<CompileRequested>
{
    private readonly ILogger<CompileRequestedConsumer> _log;
    private readonly HttpClient _http;

    public CompileRequestedConsumer(
        ILogger<CompileRequestedConsumer> log,
        HttpClient http)
    {
        _log = log;
        _http = http;
    }

    public async Task Consume(ConsumeContext<CompileRequested> context)
    {
        var msg = context.Message;

        string? workDir = null;

        try
        {
            _log.LogInformation("CompileRequested received. UserId={UserId} TaskId={TaskId}", msg.UserId, msg.TaskId);

            var source = await SourceStageLoader.LoadAsync(
                download: ct => DownloadStageAsync(
                    userId: msg.UserId,
                    taskId: msg.TaskId,
                    stage: "load",
                    fileName: null,
                    ct: ct),
                correlationId: msg.CorrelationId,
                ct: context.CancellationToken);

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

            var targets = FindTargets(extractDir);

            if (targets.Solutions.Count == 0 && targets.Projects.Count == 0)
                throw new InvalidOperationException("No solution or project files found");

            if (targets.Solutions.Count > 0)
                _log.LogInformation("Solutions: {List}", string.Join(" | ", targets.Solutions.Take(20)));

            if (targets.Projects.Count > 0)
                _log.LogInformation("Projects: {List}", string.Join(" | ", targets.Projects.Take(20)));

            var target = FindBuildTarget(extractDir);
            var targetDir = Path.GetDirectoryName(target)!;

            _log.LogInformation("Selected build target: {Target}. TargetDir={TargetDir}", target, targetDir);

            var outDir = Path.Combine(workDir, "_out");
            Directory.CreateDirectory(outDir);

            var swRestore = Stopwatch.StartNew();
            var restore = await RunDotnet(targetDir, $"restore \"{target}\" --nologo", context.CancellationToken);
            swRestore.Stop();

            _log.LogInformation(
                "dotnet restore finished in {Elapsed}ms ExitCode={ExitCode}",
                swRestore.ElapsedMilliseconds,
                restore.ExitCode);

            if (restore.ExitCode != 0)
                throw new InvalidOperationException($"dotnet restore failed: {Trim(restore.StdErr, 4000)}");

            var swBuild = Stopwatch.StartNew();
            var build = await RunDotnet(
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

            using var gate = new SemaphoreSlim(4);

            var uploads = files.Select(async path =>
            {
                await gate.WaitAsync(context.CancellationToken);
                try
                {
                    var rel = Path.GetRelativePath(outDir, path).Replace('\\', '/');
                    var bytes = await File.ReadAllBytesAsync(path, context.CancellationToken);

                    await UploadStageAsync(
                        msg.UserId,
                        msg.TaskId,
                        "build",
                        rel,
                        bytes,
                        "application/octet-stream",
                        context.CancellationToken);
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(uploads);

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
            if (workDir is not null)
                TryDelete(workDir);
        }
    }

    private async Task UploadStageAsync(
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
        var name = resp.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "source.zip";
        var ctType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new SourceStageLoader.StorageDownload(bytes, ctType, name);
    }

    private static TargetsInfo FindTargets(string root)
    {
        var solutions = Directory.GetFiles(root, "*.sln", SearchOption.AllDirectories);
        var projects = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);

        return new TargetsInfo(solutions, projects);
    }

    private static string FindBuildTarget(string root)
    {
        var sln = Directory.GetFiles(root, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
        if (sln is not null)
            return sln;

        var proj = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
        if (proj is not null)
            return proj;

        throw new InvalidOperationException("No build target found");
    }

    private static async Task<DotnetResult> RunDotnet(
        string workingDir,
        string args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi)!;

        var stdout = proc.StandardOutput.ReadToEndAsync();
        var stderr = proc.StandardError.ReadToEndAsync();

        await proc.WaitForExitAsync(ct);

        return new DotnetResult(proc.ExitCode, await stdout, await stderr);
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

    private static string Trim(string s, int max)
        => s.Length <= max ? s : s[..max];

    private sealed record DotnetResult(int ExitCode, string StdOut, string StdErr);

    private sealed record TargetsInfo(IReadOnlyList<string> Solutions, IReadOnlyList<string> Projects);
}
