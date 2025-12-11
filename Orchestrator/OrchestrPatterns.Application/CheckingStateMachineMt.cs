using MassTransit;
using OrchestrPatterns.Domain;
using SmartLearning.Contracts;

namespace OrchestrPatterns.Application;

public class CheckingStateMachineMt : MassTransitStateMachine<CheckingSaga>
{

    public State Created { get; private set; } = default!;
    public State Compiling { get; private set; } = default!;
    public State Compiled { get; private set; } = default!;
    public State Testing { get; private set; } = default!;
    public State Tested { get; private set; } = default!;
    public State Reviewing { get; private set; } = default!;
    public State Reviewed { get; private set; } = default!;
    public State Canceled { get; private set; } = default!;
    public State Failed { get; private set; } = default!;
    public State Passed { get; private set; } = default!;


    public Event<StartCompile> StartCompileEvent { get; private set; } = default!;
    public Event<StartTests> StartTestsEvent { get; private set; } = default!;
    public Event<StartReview> StartReviewEvent { get; private set; } = default!;
    public Event<Cancel> CancelEvent { get; private set; } = default!;

    public Event<CompilationFinished> CodeCompiledEvent { get; private set; } = default!;
    public Event<CompilationFailed> CompilationFailedEvent { get; private set; } = default!;
    public Event<CompileTimeout> CompileTimeoutEvent { get; private set; } = default!;

    public Event<TestsFinished> TestsFinishedEvent { get; private set; } = default!;
    public Event<TestsFailed> TestsFailedEvent { get; private set; } = default!;
    public Event<TestsTimeout> TestsTimeoutEvent { get; private set; } = default!;

    public Event<ReviewFinished> ReviewFinishedEvent { get; private set; } = default!;
    public Event<ReviewFailed> ReviewFailedEvent { get; private set; } = default!;
    public Schedule<CheckingSaga, ReviewTimeout> ReviewTmo { get; private set; } = default!;
    public CheckingStateMachineMt()
    {
        InstanceState(x => x.CurrentState);
        Schedule(() => ReviewTmo, x => x.ReviewTimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromMinutes(10);
            s.Received = r => r.CorrelateById(m => m.Message.CorrelationId);
        });

        Event(() => StartCompileEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        Event(() => StartTestsEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        Event(() => StartReviewEvent, x => { x.CorrelateById(m => m.Message.CorrelationId); x.SelectId(m => m.Message.CorrelationId); });
        Event(() => CancelEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => CodeCompiledEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompilationFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompileTimeoutEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => TestsFinishedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TestsFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TestsTimeoutEvent, x => x.CorrelateById(m => m.Message.CorrelationId));

        Event(() => ReviewFinishedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => ReviewFailedEvent, x => x.CorrelateById(m => m.Message.CorrelationId));


        Initially(
            When(StartCompileEvent)
                .Then(ctx =>
                {
                    // заполняем данные саги из сообщения StartCompile
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.TaskId = ctx.Message.TaskId;
                    ctx.Saga.Status = CheckingStatus.Created;
                })
                .TransitionTo(Compiling).Then(SetCompiling),

            When(StartTestsEvent)
                .Then(SetCreated)
                .TransitionTo(Testing).Then(SetTesting),

            When(StartReviewEvent)
                .Then(SetCreated)
                .TransitionTo(Reviewing).Then(SetReviewing)

        );


        During(Created,
            When(StartCompileEvent)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.TaskId = ctx.Message.TaskId;
                })
                .TransitionTo(Compiling).Then(SetCompiling),

            When(StartTestsEvent)
                .TransitionTo(Testing).Then(SetTesting),

            When(StartReviewEvent)
                .TransitionTo(Reviewing).Then(SetReviewing)

        );


        During(Compiling,
            Ignore(StartCompileEvent),

            When(CodeCompiledEvent)
                .TransitionTo(Compiled).Then(SetCompiled)
        );


        During(Compiled,
            When(StartTestsEvent)
                .TransitionTo(Testing).Then(SetTesting),

            When(StartReviewEvent)
                .TransitionTo(Reviewing).Then(SetReviewing),

            When(CancelEvent)
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );


        During(Testing,
            Ignore(StartTestsEvent),

            When(TestsFinishedEvent)
                .TransitionTo(Tested).Then(SetTested),

            When(TestsFailedEvent)
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(TestsTimeoutEvent)
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(CancelEvent)
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );


        During(Tested,
            When(StartReviewEvent)
                .TransitionTo(Reviewing).Then(SetReviewing),


            When(CancelEvent)
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );

        WhenEnter(Reviewing, x => x
    .ThenAsync(ctx => ctx.Publish(new ReviewRequested(ctx.Saga.CorrelationId, ctx.Saga.UserId, ctx.Saga.TaskId)))
    .Schedule(ReviewTmo, ctx => new ReviewTimeout(ctx.Saga.CorrelationId, ctx.Saga.UserId, ctx.Saga.TaskId))
);

        During(Reviewing,
            Ignore(StartReviewEvent),
            When(ReviewFinishedEvent)
                .Unschedule(ReviewTmo)
                .TransitionTo(Reviewed).Then(SetReviewed),

            When(ReviewFailedEvent)
                .Unschedule(ReviewTmo)
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(ReviewTmo.Received)
                .TransitionTo(Failed).Then(SetFailed).Finalize(),

            When(CancelEvent)
                .Unschedule(ReviewTmo)
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );


        During(Reviewed,
            When(CancelEvent)
                .TransitionTo(Canceled).Then(SetCanceled).Finalize()
        );


        During(Canceled, Ignore(CancelEvent));

        SetCompletedWhenFinalized();
    }


    static void SetCreated(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Created;
    static void SetCompiling(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Compiling;
    static void SetCompiled(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Compiled;
    static void SetTesting(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Testing;
    static void SetTested(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Tested;
    static void SetReviewing(BehaviorContext<CheckingSaga> ctx) => ctx.Saga.Status = CheckingStatus.Reviewing;
    static void SetReviewed(BehaviorContext<CheckingSaga> ctx)
    {
        ctx.Saga.Status = CheckingStatus.Reviewed;
        ctx.Saga.CompletedAt ??= DateTime.UtcNow;
    }
    static void SetCanceled(BehaviorContext<CheckingSaga> ctx)
    {
        ctx.Saga.Status = CheckingStatus.Canceled;
        ctx.Saga.CompletedAt ??= DateTime.UtcNow;
    }
    static void SetFailed(BehaviorContext<CheckingSaga> ctx)
    {
        ctx.Saga.Status = CheckingStatus.Failed;
        ctx.Saga.CompletedAt ??= DateTime.UtcNow;
    }
    static void SetPassed(BehaviorContext<CheckingSaga> ctx)
    {
        ctx.Saga.Status = CheckingStatus.Passed;
        ctx.Saga.CompletedAt ??= DateTime.UtcNow;
    }
}
