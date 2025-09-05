using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public sealed class ReviewFinishedConsumer : IConsumer<ReviewFinished>
    {
        private readonly CompletionHub _hub;
        public ReviewFinishedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<ReviewFinished> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, true); return Task.CompletedTask; }
    }
}
