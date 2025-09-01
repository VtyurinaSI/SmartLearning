using MassTransit;

namespace Orchestrator.States
{
    public class SubmissionState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;

        public string? Code { get; set; }
        public bool SkipCompile { get; set; }
        public bool SkipCheck { get; set; }
        public bool SkipReview { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Guid? StageTimeoutTokenId { get; set; }
    }
}
