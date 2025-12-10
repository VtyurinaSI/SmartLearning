using MassTransit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinIoStub;
using SmartLearning.Contracts;
using System.Text;
using static System.Net.WebRequestMethods;

namespace CompilerSevice
{
    public class CompileRequestedConsumer : IConsumer<CompileRequested>
    {
        private readonly ILogger<CompileRequestedConsumer> _log;
        private readonly IObjectStorageRepository _repo;

        public CompileRequestedConsumer(IObjectStorageRepository repo, ILogger<CompileRequestedConsumer> log)
        {
            _repo = repo;
            _log = log;
        }
        public async Task Consume(ConsumeContext<CompileRequested> context)
        {
            _log.LogInformation("Compile requested: CorrelationId={Cid}, UserId={UserId}, TaskId={TaskId}",
                context.Message.CorrelationId, context.Message.UserId, context.Message.TaskId);

            var origCode = await _repo.ReadOrigCodeAsync(context.Message.CorrelationId, context.CancellationToken);
            if (origCode is null)
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
    }
}
