using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public sealed class CompileFinishedConsumers : IConsumer<CompilationFinished>
    {
        private readonly CompletionHub _hub;
        public CompileFinishedConsumers(CompletionHub hub) => _hub = hub;
        public Task Consume(ConsumeContext<CompilationFinished> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, true); return Task.CompletedTask; }
    }
}
