using MassTransit;
using ProgressService;
using SmartLearning.Contracts;

internal class UpdateProgressConsumer : IConsumer<UpdateProgress>
{
    private readonly ILogger<UpdateProgressConsumer> _log;
    private readonly IUserProgressRepository _repo;

    public UpdateProgressConsumer(IUserProgressRepository repo, ILogger<UpdateProgressConsumer> log)
    {
        _repo = repo;
        _log = log;
    }
    public async Task Consume(ConsumeContext<UpdateProgress> context)
    {
        await _repo.SaveCheckingAsync(context.Message.UserId,
            context.Message.TaskId,
            context.Message.IsCompiledSuccess,
            context.Message.IsTestedSuccess,
            context.Message.IsReviewedSuccess,
            context.CancellationToken
            );
        _log.LogInformation("Progress updated for user {UserId}, task {TaskId}: Compile={Compile}, Test={Test}, Review={Review}",
            context.Message.UserId,
            context.Message.TaskId,
            context.Message.IsCompiledSuccess,
            context.Message.IsTestedSuccess,
            context.Message.IsReviewedSuccess
        );
    }
}