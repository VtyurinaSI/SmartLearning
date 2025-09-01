using Contracts;
using MassTransit;

namespace Orchestrator.States
{
    internal class SubmissionStateMachine : MassTransitStateMachine<SubmissionState>
    {
        public State Compiling { get; private set; } = default!;
        public State Checking { get; private set; } = default!;
        public State Reviewing { get; private set; } = default!;
        public State Completed { get; private set; } = default!;
        public State Failed { get; private set; } = default!;

        public Event<SubmissionRequested> SubmissionRequestedEvent { get; private set; } = default!;
        public Event<CompileCompleted> CompileCompletedEvent { get; private set; } = default!;
        public Event<CodeCheckCompleted> CodeCheckCompletedEvent { get; private set; } = default!;
        public Event<ReviewCompleted> ReviewCompletedEvent { get; private set; } = default!;
        public Event<StageFailed> StageFailedEvent { get; private set; } = default!;

        public Schedule<SubmissionState, TimeoutExpired> StageTimeout { get; private set; } = default!;

    }
}
