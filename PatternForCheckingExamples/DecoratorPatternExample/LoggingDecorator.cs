namespace DecoratorPatternExample;

public sealed class LoggingDecorator : IComponent
{
    private readonly BasicComponent _inner;

    public LoggingDecorator(BasicComponent inner)
    {
        _inner = inner;
    }

    public string Render()
    {
        return _inner.Render();
    }
}
