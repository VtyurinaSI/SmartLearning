using MassTransit;
using SmartLearning.Contracts;

namespace UserService
{
    public class UserCreatedConsumer : IConsumer<UserCreated>
    {
        private readonly UserCreatedHandler _handler;

        public UserCreatedConsumer(UserCreatedHandler handler)
        {
            _handler = handler;
        }

        public Task Consume(ConsumeContext<UserCreated> context)
        {
            return _handler.HandleAsync(context.Message, context.CancellationToken);
        }
    }
}
