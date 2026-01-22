using MassTransit;
using SmartLearning.Contracts;

namespace Orchestrator.Application.Consumers
{
    public class WrongProjectStructureConsumer : IConsumer<WrongProjectStructure>
    {
        private readonly CompletionHub _hub;
        public WrongProjectStructureConsumer(CompletionHub hub) => _hub = hub;

        public Task Consume(ConsumeContext<WrongProjectStructure> ctx)
        {
            _hub.SetCompleted(ctx.Message.CorrelationId, false, ctx.Message.Message);
            return Task.CompletedTask;
        }
    }
}

