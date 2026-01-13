using MassTransit;
using SmartLearning.Contracts;

namespace Orchestrator.Application.Consumers
{
    public sealed class ReviewFailedConsumer : IConsumer<ReviewFailed>
    {
        private readonly CompletionHub _hub;
        public ReviewFailedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<ReviewFailed> ctx)
        { _hub.SetCompleted(ctx.Message.CorrelationId, false, ctx.Message.Result); return Task.CompletedTask; }
    }
}

