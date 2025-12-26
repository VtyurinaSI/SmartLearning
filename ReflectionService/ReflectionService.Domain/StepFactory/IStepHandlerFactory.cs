using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.StepFactory;

public interface IStepHandlerFactory
{
    HandlerTemplateBase Create(ManifestStep step);
}