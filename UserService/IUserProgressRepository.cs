using SmartLearning.Contracts;

public interface IUserProgressRepository
{
    Task CreateUserAsync(UserCreated user, CancellationToken ct);

}