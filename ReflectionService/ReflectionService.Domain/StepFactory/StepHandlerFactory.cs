using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.StepFactory;

public sealed class StepHandlerFactory : IStepHandlerFactory
{
    private readonly IReadOnlyDictionary<string, IStepHandlerRegistration> _map;

    public StepHandlerFactory(IEnumerable<IStepHandlerRegistration> registrations)
    {
        if (registrations is null) throw new ArgumentNullException(nameof(registrations));

        var dict = new Dictionary<string, IStepHandlerRegistration>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in registrations)
        {
            if (r is null) continue;

            if (dict.ContainsKey(r.Operation))
                throw new InvalidOperationException($"Duplicate step registration for operation '{r.Operation}'.");

            dict.Add(r.Operation, r);
        }

        _map = dict;
    }

    public HandlerTemplateBase Create(ManifestStep step)
    {
        if (step is null) throw new ArgumentNullException(nameof(step));

        if (!_map.TryGetValue(step.Operation, out var reg))
        {
            throw new NotSupportedException(
                $"Unknown operation '{step.Operation}' (stepId='{step.Id}'). " +
                $"Supported: {string.Join(", ", _map.Keys.OrderBy(x => x))}");
        }

        return reg.Create();
    }
}