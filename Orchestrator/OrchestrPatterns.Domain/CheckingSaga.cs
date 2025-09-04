using MassTransit;

namespace OrchestrPatterns.Domain
{
    public class CheckingSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = default!;

        public CheckingStatus Status { get; set; } = CheckingStatus.Created;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Guid? ReviewTimeoutTokenId { get; set; }
    }
}
