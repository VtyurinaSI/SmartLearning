namespace SingletonPatternExample;

public sealed class Singleton
{
    private static readonly Singleton _instance = new();

    public static Singleton Instance => _instance;

    public static Singleton GetInstance() => _instance;

    internal Singleton()
    {
    }

    public string AppName { get; } = "SmartLearning";
}
