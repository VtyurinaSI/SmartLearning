using MassTransit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinIoStub;
using SmartLearning.Contracts;
using System.ComponentModel.Design.Serialization;
using System.Net;
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
        }
        public async Task Consume(ConsumeContext<CompileRequested> context)
        {
            _log.LogInformation("Compile requested: CorrelationId={Cid}, UserId={UserId}, TaskId={TaskId}",
                context.Message.CorrelationId, context.Message.UserId, context.Message.TaskId);

            try
            {
                var url = $"/objects/{"load"}/file?userId={context.Message.UserId}&taskId={context.Message.TaskId}";

                using var resp = await _http.GetAsync(url, context.CancellationToken);
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    _log.LogError("ObjectStorage returned 404 for {Cid}", context.Message.CorrelationId);
                    return;
                }
                resp.EnsureSuccessStatusCode();

                var bytes = await resp.Content.ReadAsByteArrayAsync(context.CancellationToken);
                var origCode = Encoding.UTF8.GetString(bytes);

                if (string.IsNullOrEmpty(origCode))
                {
                    _log.LogError("Compiling code for {Cid} is empty", context.Message.CorrelationId);
                    return;
                }

                _log.LogDebug("Compiling code for {Cid}: \n{Code}", context.Message.CorrelationId, origCode);

                var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

                var tree = CSharpSyntaxTree.ParseText(origCode, parse);

                var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
                var references = tpa
                    .Split(Path.PathSeparator)
                    .Select(p => MetadataReference.CreateFromFile(p))
                    .ToArray();

                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

                var compilation = CSharpCompilation.Create(
                    "MyAssembly",
                    new[] { tree },
                    references,
                    options);

                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                StringBuilder sb = new();
                sb.Append($"Compilation ");
                sb.Append(result.Success ? "Success" : "Failed");
                sb.Append(". ");
                sb.AppendLine(string.Join(",", result.Diagnostics
                    .GroupBy(r => r.Severity)
                    .Select(g => $"{g.Key} - {g.Count()}")));
                foreach (var d in result.Diagnostics)
                    sb.AppendLine(d.ToString());

                _log.LogInformation(sb.ToString());
                if (result.Success)
                    await context.Publish(new CompilationFinished(context.Message.CorrelationId));
                else
                    await context.Publish(new CompilationFailed(context.Message.CorrelationId));
                await _repo.SaveCompilationAsync(context.Message.CorrelationId, sb.ToString(), context.CancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error while reading source from ObjectStorage for {Cid}", context.Message.CorrelationId);
                await context.Publish(new CompilationFailed(context.Message.CorrelationId));
            }
        }
    }
}
