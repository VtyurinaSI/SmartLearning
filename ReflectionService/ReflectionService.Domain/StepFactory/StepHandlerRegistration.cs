namespace ReflectionService.Domain.StepFactory;

public sealed class StepHandlerRegistration<THandler> : IStepHandlerRegistration
    where THandler : HandlerTemplateBase
{
    public string Operation { get; }

    private readonly Func<THandler> _factory;

    public StepHandlerRegistration(string operation, Func<THandler> factory)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation name is empty.", nameof(operation));

        Operation = operation;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public HandlerTemplateBase Create() => _factory();
}