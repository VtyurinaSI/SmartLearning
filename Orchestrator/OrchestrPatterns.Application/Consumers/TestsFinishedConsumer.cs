using MassTransit;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application.Consumers
{
    public class TestsFinishedConsumer : IConsumer<TestsFinished>
    {
        private readonly CompletionHub _hub;
        public TestsFinishedConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<TestsFinished> ctx)
        {
            _hub.SetCompleted(ctx.Message.CorrelationId, true, ctx.Message.Result);
            return Task.CompletedTask;
        }
    }
}
