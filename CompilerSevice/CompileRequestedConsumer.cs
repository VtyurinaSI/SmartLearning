using MassTransit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinIoStub;
using SmartLearning.Contracts;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace CompilerSevice
{
    public class CompileRequestedConsumer : IConsumer<CompileRequested>
    {
        private readonly ILogger<CompileRequestedConsumer> _log;
        private readonly IObjectStorageRepository _repo;
        private readonly HttpClient _http;

        public CompileRequestedConsumer(IObjectStorageRepository repo, ILogger<CompileRequestedConsumer> log, HttpClient http)
        {
            _repo = repo;
            _log = log;
            _http = http;
            _log.LogInformation("Создан {obj}, http-client: {cl}", nameof(CompileRequestedConsumer), _http.BaseAddress);
        }

        public async Task Consume(ConsumeContext<CompileRequested> context)
        {
            _log.LogInformation("Compile requested: CorrelationId={Cid}, UserId={UserId}, TaskId={TaskId}",
                context.Message.CorrelationId, context.Message.UserId, context.Message.TaskId);

            try
            {
                var origCode = await LoadSourceAsync(context);
                if (string.IsNullOrWhiteSpace(origCode))
                {
                    _log.LogError("Compiling code for {Cid} is empty", context.Message.CorrelationId);
                    return;
                }

                _log.LogDebug("Compiling code for {Cid}: \n{Code}", context.Message.CorrelationId, origCode);

                var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
                var syntaxTree = CSharpSyntaxTree.ParseText(origCode, parseOptions);
                var references = GetFrameworkReferences();

                var dllCompilation = CreateCompilation(
                    syntaxTree,
                    references,
                    OutputKind.DynamicallyLinkedLibrary,
                    "UserLibrary");

                var dllEmit = EmitToArray(dllCompilation);

                EmitResultInfo? exeEmit = null;
                try
                {
                    var exeCompilation = CreateCompilation(
                        syntaxTree,
                        references,
                        OutputKind.ConsoleApplication,
                        "UserProgram");

                    exeEmit = EmitToArray(exeCompilation);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Exe compilation failed for {Cid}, dll compilation attempted.", context.Message.CorrelationId);
                }

                var sb = BuildDiagnosticsLog(dllEmit);
                _log.LogInformation(sb.ToString());

                if (dllEmit.Success)
                    await context.Publish(new CompilationFinished(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
                else
                    await context.Publish(new CompilationFailed(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));

                await _repo.SaveCompilationAsync(context.Message.CorrelationId, sb.ToString(), context.CancellationToken);

                if (dllEmit.Success && dllEmit.Bytes is { Length: > 0 } dllBytes)
                    await UploadAssemblyAsync("build", "program.dll", dllBytes, context);

                if (exeEmit is { Success: true, Bytes: { Length: > 0 } exeBytes })
                    await UploadAssemblyAsync("build", "program.exe", exeBytes, context);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error while compiling source for {Cid}", context.Message.CorrelationId);
                await context.Publish(new CompilationFailed(context.Message.CorrelationId,
                        context.Message.UserId, context.Message.TaskId));
            }
        }

        private async Task<string> LoadSourceAsync(ConsumeContext<CompileRequested> context)
        {
            var url = $"/objects/load/file?userId={context.Message.UserId}&taskId={context.Message.TaskId}&fileName={"file.txt"}";

            using var resp = await _http.GetAsync(url, context.CancellationToken);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                _log.LogError("ObjectStorage returned 404 for {Cid}", context.Message.CorrelationId);
                return string.Empty;
            }

            resp.EnsureSuccessStatusCode();

            var bytes = await resp.Content.ReadAsByteArrayAsync(context.CancellationToken);
            return Encoding.UTF8.GetString(bytes);
        }

        private static MetadataReference[] GetFrameworkReferences()
        {
            var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
            return tpa
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToArray();
        }

        private static CSharpCompilation CreateCompilation(
            SyntaxTree tree,
            MetadataReference[] references,
            OutputKind kind,
            string assemblyName)
        {
            var options = new CSharpCompilationOptions(kind);
            return CSharpCompilation.Create(
                assemblyName,
                new[] { tree },
                references,
                options);
        }

        private static EmitResultInfo EmitToArray(CSharpCompilation compilation)
        {
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            byte[]? bytes = null;
            if (result.Success)
            {
                ms.Position = 0;
                bytes = ms.ToArray();
            }

            return new EmitResultInfo(result.Success, bytes, result.Diagnostics);
        }

        private static StringBuilder BuildDiagnosticsLog(EmitResultInfo emitInfo)
        {
            var result = emitInfo.Diagnostics;

            var sb = new StringBuilder();
            sb.Append("Compilation ");
            sb.Append(emitInfo.Success ? "Success" : "Failed");
            sb.Append(". ");
            sb.AppendLine(string.Join(",",
                result.GroupBy(r => r.Severity)
                      .Select(g => $"{g.Key} - {g.Count()}")));

            foreach (var d in result)
                sb.AppendLine(d.ToString());

            return sb;
        }

        private async Task UploadAssemblyAsync(string stage, string fileName, byte[] bytes, ConsumeContext<CompileRequested> context)
        {
            var url = $"/objects/{stage}/file?userId={context.Message.UserId}&taskId={context.Message.TaskId}&fileName={Uri.EscapeDataString(fileName)}";
            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var resp = await _http.PostAsync(url, content, context.CancellationToken);
            resp.EnsureSuccessStatusCode();
        }

        private readonly record struct EmitResultInfo(
            bool Success,
            byte[]? Bytes,
            IReadOnlyList<Diagnostic> Diagnostics);
    }
}
