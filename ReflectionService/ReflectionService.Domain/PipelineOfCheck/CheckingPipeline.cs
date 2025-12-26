using System.Reflection;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.StepFactory;

namespace ReflectionService.Domain.PipelineOfCheck;

public sealed class CheckingPipeline
{
    private readonly IStepHandlerFactory _factory;

    private readonly List<(ManifestStep Step, HandlerTemplateBase Handler)> _pipeline = new();

    public CheckingPipeline(IStepHandlerFactory factory) => _factory = factory;

    public void SetPipeline(CheckManifest rules)
    {
        _pipeline.Clear();

        foreach (var step in rules.Steps)
        {
            var handler = _factory.Create(step);
            _pipeline.Add((step, handler));
        }
    }

    public CheckingContext ExecutePipeline(Assembly userAssembly, ManifestTarget target)
    {
        var ctx = new CheckingContext(userAssembly, target);

        foreach (var (step, handler) in _pipeline)
        {
            handler.Execute(ctx, step);

            if (step.StopOnFail && ctx.StepResults.Count > 0 && ctx.StepResults[^1].Passed == false)
                break;
        }

        return ctx;
    }
}
