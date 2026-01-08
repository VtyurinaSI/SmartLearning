using Microsoft.Extensions.Logging;
using ReflectionService.Domain;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.StepFactory;
using System.Reflection;

namespace ReflectionService.Domain.PipelineOfCheck;

public sealed class CheckingPipeline
{
    private readonly IStepHandlerFactory _factory;
    private readonly ILogger<CheckingPipeline> _log;

    private readonly List<(ManifestStep Step, HandlerTemplateBase Handler)> _pipeline = new();
    private CheckManifest? _manifest;

    public CheckingPipeline(IStepHandlerFactory factory, ILogger<CheckingPipeline> log)
    {
        _factory = factory;
        _log = log;
    }

    public void SetPipeline(CheckManifest rules)
    {
        _manifest = rules;
        _pipeline.Clear();

        _log.LogInformation("Pipeline set: {ops}", string.Join(", ", rules.Steps.Select(s => $"{s.Id}:{s.Operation}")));

        foreach (var step in rules.Steps)
        {
            var handler = _factory.Create(step);
            _pipeline.Add((step, handler));
        }
    }

    public CheckingContext ExecutePipeline(Assembly userAssembly, ManifestTarget target)
    {
        var ctx = new CheckingContext(userAssembly, target, _manifest);

        _log.LogInformation("Ctx manifest steps={n}", ctx.Manifest?.Steps.Length ?? 0);
        _log.LogInformation("Executing pipeline: {ops}", string.Join(", ", _pipeline.Select(p => $"{p.Step.Id}:{p.Step.Operation}")));

        foreach (var (step, handler) in _pipeline)
        {
            handler.Execute(ctx, step);

            if (step.StopOnFail && ctx.StepResults.Count > 0 && !ctx.StepResults[^1].Passed)
                break;
        }

        return ctx;
    }
}
