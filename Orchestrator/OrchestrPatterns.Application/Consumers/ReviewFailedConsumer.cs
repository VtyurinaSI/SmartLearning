using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public sealed class ReviewFailedConsumer : IConsumer<ReviewFailed>
    {
        private readonly CompletionHub _hub;
        public ReviewFailedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<ReviewFailed> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, false); return Task.CompletedTask; }
    }
}
