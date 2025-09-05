using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public sealed class CompileFailedConsumer : IConsumer<CompilationFailed>
    {
        private readonly CompletionHub _hub;
        public CompileFailedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<CompilationFailed> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, false); return Task.CompletedTask; }
    }
}
