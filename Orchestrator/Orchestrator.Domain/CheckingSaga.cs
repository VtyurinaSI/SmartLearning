using MassTransit;
using SmartLearning.Contracts;

namespace Orchestrator.Domain
{
    public class CheckingSaga : SagaStateMachineInstance
    {
        public CheckingData Results { get; set; } = default!;

        public UpdateProgress MakeUpdateMessage() => 
            new(Results.UserId, 
                Results.TaskId, 
                Results.TaskName, 
                Results.IsCompiledSuccess,
                Results.IsTestedSuccess, 
                Results.IsReviewedSucces,
                CorrelationId,
                Results.IsCheckingFinished,
                Results.CheckResult, 
                Results.CompileMsg, 
                Results.TestMsg,
                Results.ReviewMsg);
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = default!;

        public CheckingStatus Status { get; set; } = CheckingStatus.Compiling;

    }
}

