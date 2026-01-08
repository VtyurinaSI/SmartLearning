using SmartLearning.Contracts;

namespace ProgressService
{
    public sealed class ProgressUpdateService
    {
        private readonly IUserProgressRepository _repo;
        private readonly ILogger<ProgressUpdateService> _log;

        public ProgressUpdateService(IUserProgressRepository repo, ILogger<ProgressUpdateService> log)
        {
            _repo = repo;
            _log = log;
        }

        public async Task UpdateAsync(UpdateProgress message, Guid? correlationId, CancellationToken ct)
        {
            await _repo.SaveCheckingAsync(
                message.UserId,
                message.TaskId,
                message.TaskName ?? string.Empty,
                message.IsCompiledSuccess,
                message.IsTestedSuccess,
                message.IsReviewedSuccess,
                message.CorrelationId ?? correlationId,
                message.CheckResult,
                message.CompileMsg,
                message.TestMsg,
                message.ReviewMsg,
                ct);
            _log.LogInformation("Progress updated for user {UserId}, task {TaskId}: Compile={Compile}, Test={Test}, Review={Review}, Result={Result}",
                message.UserId,
                message.TaskId,
                message.IsCompiledSuccess,
                message.IsTestedSuccess,
                message.IsReviewedSuccess,
                message.CheckResult
            );
        }
    }
}
