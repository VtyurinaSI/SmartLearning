using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public class TestsFailedConsumer : IConsumer<TestsFailed>
    {
        private readonly CompletionHub _hub;
        public TestsFailedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<TestsFailed> ctx)
        {
            _hub.SetCompleted(ctx.Message.CorrelationId, false);
            return Task.CompletedTask;
        }
    }
}
