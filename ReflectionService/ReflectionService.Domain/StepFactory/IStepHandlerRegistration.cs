namespace ReflectionService.Domain.StepFactory;

public interface IStepHandlerRegistration
{
    string Operation { get; }
    HandlerTemplateBase Create();
}