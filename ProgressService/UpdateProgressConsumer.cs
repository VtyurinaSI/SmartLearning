using MassTransit;
using SmartLearning.Contracts;

internal class UpdateProgressConsumer : IConsumer<UpdateProgress>
{
    private readonly ProgressService.ProgressUpdateService _service;

    public UpdateProgressConsumer(ProgressService.ProgressUpdateService service)
    {
        _service = service;
    }

    public Task Consume(ConsumeContext<UpdateProgress> context)
    {
        Guid? correlation = context.CorrelationId == Guid.Empty ? null : context.CorrelationId;
        return _service.UpdateAsync(context.Message, correlation, context.CancellationToken);
    }
}
