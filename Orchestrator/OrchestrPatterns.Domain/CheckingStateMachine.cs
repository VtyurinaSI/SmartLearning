using Stateless;

namespace OrchestrPatterns.Domain
{
    public class CheckingStateMachine
    {
        internal static StateMachine<CheckingStatus, CheckingTrigger> Create(
            Func<CheckingStatus> getStatus,
            Action<CheckingStatus> setStatus,
            Action<CheckingTrigger, CheckingStatus, CheckingStatus>? onTransition = null)
        {
            var sm = new StateMachine<CheckingStatus, CheckingTrigger>(getStatus, setStatus);

            
            sm.Configure(CheckingStatus.Created)
                .Permit(CheckingTrigger.StartCompile, CheckingStatus.Compiling)
                .Permit(CheckingTrigger.StartTests, CheckingStatus.Testing)
                .Permit(CheckingTrigger.StartReview, CheckingStatus.Reviewing)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            
            sm.Configure(CheckingStatus.Compiling)
                .Ignore(CheckingTrigger.StartCompile)
                .Permit(CheckingTrigger.CodeCompiled, CheckingStatus.Compiled)
                .Permit(CheckingTrigger.CompilationFailed, CheckingStatus.Failed)
                .Permit(CheckingTrigger.CompileTimeout, CheckingStatus.Failed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            
            sm.Configure(CheckingStatus.Compiled)
                .Permit(CheckingTrigger.StartTests, CheckingStatus.Testing)
                .Permit(CheckingTrigger.StartReview, CheckingStatus.Reviewing)
                .Permit(CheckingTrigger.Finalize, CheckingStatus.Passed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            
            sm.Configure(CheckingStatus.Testing)
                .Ignore(CheckingTrigger.StartTests)
                .Permit(CheckingTrigger.TestsFinished, CheckingStatus.Tested)
                .Permit(CheckingTrigger.TestsFailed, CheckingStatus.Failed)
                .Permit(CheckingTrigger.TestsTimeout, CheckingStatus.Failed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            
            sm.Configure(CheckingStatus.Tested)
                .Permit(CheckingTrigger.StartReview, CheckingStatus.Reviewing)
                .Permit(CheckingTrigger.Finalize, CheckingStatus.Passed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

          
            sm.Configure(CheckingStatus.Reviewing)
                .Ignore(CheckingTrigger.StartReview)
                .Permit(CheckingTrigger.ReviewFinished, CheckingStatus.Reviewed)
                .Permit(CheckingTrigger.ReviewFailed, CheckingStatus.Failed)
                .Permit(CheckingTrigger.ReviewTimeout, CheckingStatus.Failed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            sm.Configure(CheckingStatus.Reviewed)
                .Permit(CheckingTrigger.Finalize, CheckingStatus.Passed)
                .Permit(CheckingTrigger.Cancel, CheckingStatus.Canceled);

            
            sm.Configure(CheckingStatus.Canceled)
                .Ignore(CheckingTrigger.Cancel);

            sm.Configure(CheckingStatus.Failed)
                .Ignore(CheckingTrigger.Finalize)
                .Ignore(CheckingTrigger.Cancel);

            sm.Configure(CheckingStatus.Passed)
                .Ignore(CheckingTrigger.Finalize)
                .Ignore(CheckingTrigger.Cancel);

            sm.OnTransitioned(t => onTransition?.Invoke(t.Trigger, t.Source, t.Destination));
            return sm;
        }
    }
}
