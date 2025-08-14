using Stateless;

namespace OrchestrPatterns.Domain
{
    public class Checking
    {
        public Checking(Action<CheckingTrigger, CheckingStatus, CheckingStatus>? inTransitionAct)
        {
            inTransitionAction = inTransitionAct;
        }
        public Checking() { }
        private StateMachine<CheckingStatus, CheckingTrigger>? fsm;
        public CheckingStatus Status { get; private set; } = CheckingStatus.Created;
        private readonly Action<CheckingTrigger, CheckingStatus, CheckingStatus>? inTransitionAction;
        public void Start(Action<CheckingTrigger, CheckingStatus, CheckingStatus>? inTransitionAct)
        {
            fsm = CheckingStateMachine.Create(() => Status,
                s => Status = s,
                (trigger, from, to) =>
                {
                    if (inTransitionAct is not null)
                        inTransitionAct(trigger, from, to);
                });
            TryFireChecking(CheckingTrigger.StartCompile);
        }
        public bool TryFireChecking(CheckingTrigger tr)
        {
            if (!fsm!.CanFire(tr))
                return false;
            fsm.Fire(tr);
            return true;
        }
    }
}
