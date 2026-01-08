namespace CommandPatternExample;

public sealed class CommandInvoker
{
    private readonly ICommand _command;

    public CommandInvoker(ICommand command)
    {
        _command = command;
    }

    public void Run()
    {
        _command.Execute();
    }
}
