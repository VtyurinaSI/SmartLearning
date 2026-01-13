using MassTransit;
using SmartLearning.Contracts;

namespace Orchestrator.Application.Consumers
{
    public sealed class CompileFinishedConsumer : IConsumer<CompilationFinished>
    {
        private readonly CompletionHub _hub;
        public CompileFinishedConsumer(CompletionHub hub) => _hub = hub;
        public Task Consume(ConsumeContext<CompilationFinished> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, true, ctx.Message.Result); return Task.CompletedTask; }
    }
}


