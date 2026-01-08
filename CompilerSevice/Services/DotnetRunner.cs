using System.Diagnostics;

namespace CompilerSevice.Services;

public sealed class DotnetRunner
{
    public async Task<DotnetResult> RunAsync(string workingDir, string args, CancellationToken ct)
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
}

public sealed record DotnetResult(int ExitCode, string StdOut, string StdErr);
