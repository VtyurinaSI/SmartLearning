using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps
{
    public interface IStepHandlerFactory
    {
        HandlerTemplateBase Create(ManifestStep step);
    }
}
