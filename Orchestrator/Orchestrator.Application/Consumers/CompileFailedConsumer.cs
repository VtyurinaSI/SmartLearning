using MassTransit;
using SmartLearning.Contracts;

namespace Orchestrator.Application.Consumers
{
    public sealed class CompileFailedConsumer : IConsumer<CompilationFailed>
    {
        private readonly CompletionHub _hub;
        public CompileFailedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<CompilationFailed> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, false, ctx.Message.Result); return Task.CompletedTask; }
    }
}

