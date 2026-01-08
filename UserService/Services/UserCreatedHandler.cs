using SmartLearning.Contracts;

namespace UserService
{
    public sealed class UserCreatedHandler
    {
        private readonly IUserProgressRepository _repo;
        private readonly ILogger<UserCreatedHandler> _log;

        public UserCreatedHandler(IUserProgressRepository repo, ILogger<UserCreatedHandler> log)
        {
            _repo = repo;
            _log = log;
        }

        public async Task HandleAsync(UserCreated message, CancellationToken ct)
        {
            await _repo.CreateUserAsync(message, ct);
            _log.LogInformation("User created: {UserId}, {UserName}",
                message.UserId,
                message.Login
            );
        }
    }
}
