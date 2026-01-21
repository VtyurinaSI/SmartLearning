using MassTransit;
using Orchestrator.Domain;
using SmartLearning.Contracts;

namespace Orchestrator.Application;

public class CheckingStateMachineMt : MassTransitStateMachine<CheckingSaga>
{
    public CheckingStateMachineMt()
    {
        InstanceState(x => x.CurrentState);

        Event(() => CheckRequestedEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });

        //Event(() => StartCompileEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        //Event(() => StartTestsEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        //Event(() => StartReviewEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        Event(() => CancelEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => CodeCompiledEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompilationFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompileTimeoutEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => TestsFinishedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TestsFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TestsTimeoutEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => ReviewFinishedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => ReviewFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => ReviewTimeoutEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(CheckRequestedEvent)
                .Then(ctx => ctx.Saga.Results = new(ctx.Message.UserId, ctx.Message.TaskId, ctx.Message.TaskName))
                .ThenAsync(ctx => ctx.Publish(new CompileRequested(ctx.Saga.CorrelationId, ctx.Saga.Results.UserId, ctx.Saga.Results.TaskId)))
                .TransitionTo(Compiling)
        );


        WhenEnter(Compiling, x => x.Then(SetCompiling));

        During(Compiling,
             When(CodeCompiledEvent)
             .Then(ctx =>
             {
                 ctx.Saga.Results.CompileMsg = ctx.Message.Result;
                 ctx.Saga.Results.IsCompiledSuccess = true;
             })
             .TransitionTo(Testing),

             When(CompilationFailedEvent)
                 .Then(ctx => ctx.Saga.Results.CompileMsg = ctx.Message.Result)
                 .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                 .TransitionTo(Failed).Then(SetFailed).Finalize(),

             When(CompileTimeoutEvent)
             .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                 .TransitionTo(Failed).Then(SetFailed).Finalize(),

             When(CancelEvent)
             .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                 .TransitionTo(Canceled).Then(SetCanceled).Finalize()
         );

        WhenEnter(Testing, x => x.Then(SetTesting).ThenAsync(ctx =>
           ctx.Publish(new TestRequested(ctx.Saga.CorrelationId, ctx.Saga.Results.UserId, ctx.Saga.Results.TaskId))));

        During(Testing,
            When(TestsFinishedEvent)
                .Then(ctx =>
                    {
                        ctx.Saga.Results.TestMsg = ctx.Message.Result;
                        ctx.Saga.Results.IsTestedSuccess = true;
                    })
                .TransitionTo(Reviewing),

            When(TestsFailedEvent)
            .Then(ctx => ctx.Saga.Results.TestMsg = ctx.Message.Result)
            .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(TestsTimeoutEvent)
                .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(CancelEvent)
                .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );


        WhenEnter(Reviewing, x => x.Then(SetReviewing)
            .ThenAsync(async ctx => await ctx.Publish(new ReviewRequested(ctx.Saga.CorrelationId, ctx.Saga.Results.UserId, ctx.Saga.Results.TaskId, ctx.Saga.Results.TaskName ?? "Any pattern"))));

        During(Reviewing,
            When(ReviewFinishedEvent)
            .Then(ctx =>
            {
                ctx.Saga.Results.ReviewMsg = ctx.Message.Result;
                ctx.Saga.Results.IsReviewedSucces = true;
            })
                .TransitionTo(Passed).Then(SetPassed).Finalize(),

            When(ReviewFailedEvent)
            .Then(ctx => ctx.Saga.Results.ReviewMsg = ctx.Message.Result)
            .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(ReviewTimeoutEvent)
            .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(CancelEvent)
            .ThenAsync(ctx => ctx.Publish(ctx.Saga.MakeUpdateMessage()))
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );




        During(Canceled, Ignore(CancelEvent));

        SetCompletedWhenFinalized();
    }

    #region states

    public State Compiling { get; private set; } = default!;

    public State Testing { get; private set; } = default!;

    public State Reviewing { get; private set; } = default!;

    public State Passed { get; private set; } = default!;
    public State Failed { get; private set; } = default!;
    public State Canceled { get; private set; } = default!;
    #endregion


    #region events
    public Event<StartChecking> CheckRequestedEvent { get; private set; } = default!;

    //public Event<CompileRequested> StartCompileEvent { get; private set; } = default!;
    public Event<CompilationFinished> CodeCompiledEvent { get; private set; } = default!;
    public Event<CompilationFailed> CompilationFailedEvent { get; private set; } = default!;
    public Event<CompileTimeout> CompileTimeoutEvent { get; private set; } = default!;

    //public Event<TestRequested> StartTestsEvent { get; private set; } = default!;
    public Event<TestsFailed> TestsFailedEvent { get; private set; } = default!;
    public Event<TestsFinished> TestsFinishedEvent { get; private set; } = default!;
    public Event<TestsTimeout> TestsTimeoutEvent { get; private set; } = default!;

    //public Event<ReviewRequested> StartReviewEvent { get; private set; } = default!;
    public Event<ReviewFailed> ReviewFailedEvent { get; private set; } = default!;
    public Event<ReviewFinished> ReviewFinishedEvent { get; private set; } = default!;
    public Event<ReviewTimeout> ReviewTimeoutEvent { get; private set; } = default!;

    public Event<Cancel> CancelEvent { get; private set; } = default!;
    #endregion

    static void SetCanceled(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Canceled;
    static void SetCompiling(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Compiling;

    static void SetFailed(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Failed;

    static void SetPassed(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Passed;


    static void SetReviewing(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Reviewing;


    static void SetTesting(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Testing;
}


