using MassTransit;
using SmartLearning.Contracts;

namespace UserService
{
    public class UserCreatedConsumer : IConsumer<UserCreated>
    {
        private readonly ILogger<UserCreatedConsumer> _log;
        private readonly IUserProgressRepository _repo;

        public UserCreatedConsumer(IUserProgressRepository repo, ILogger<UserCreatedConsumer> log)
        {
            _repo = repo;
            _log = log;
        }
        public async Task Consume(ConsumeContext<UserCreated> context)
        {
            await _repo.CreateUserAsync(context.Message, context.CancellationToken);
            _log.LogInformation("User created: {UserId}, {UserName}",
                context.Message.UserId,
                context.Message.Login
            );
        }
    }
}
