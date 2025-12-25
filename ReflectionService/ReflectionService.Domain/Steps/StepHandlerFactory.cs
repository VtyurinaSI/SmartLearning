using Microsoft.Extensions.DependencyInjection;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.Steps.AssertExists;
using ReflectionService.Domain.Steps.AssertMemberSignature;
using ReflectionService.Domain.Steps.AssertModifiers;
using ReflectionService.Domain.Steps.CountTypes;
using ReflectionService.Domain.Steps.FindCtor;
using ReflectionService.Domain.Steps.FindImplementations;
using ReflectionService.Domain.Steps.FindInterface;
using ReflectionService.Domain.Steps.FindMembers;
using ReflectionService.Domain.Steps.FindTypesStep;
using ReflectionService.Domain.Steps.HasDependency;
using ReflectionService.Domain.Steps.PickOne;

namespace ReflectionService.Domain.Steps;

public sealed class StepHandlerFactory : IStepHandlerFactory
{
    private readonly IServiceProvider _sp;

    public StepHandlerFactory(IServiceProvider sp) => _sp = sp;

    private static readonly IReadOnlyDictionary<string, Type> Map =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["FindTypes"] = typeof(FindTypes),
            ["CountTypes"] = typeof(CountTypesHandler),
            ["FindInterfaces"] = typeof(FindInterfacesHandler),
            ["FindCtorConsumers"] = typeof(FindCtorConsumersHandler),
            ["FindImplementations"] = typeof(FindImplementationsHandler),
            ["FindMembers"] = typeof(FindMembersHandler),
            ["PickOne"] = typeof(PickOneHandler),
            ["HasDependency"] = typeof(HasDependencyHandler),
            ["HasCollectionDependency"] = typeof(HasCollectionDependencyHandler),
            ["AssertExists"] = typeof(AssertExistsHandler),
            ["AssertModifiers"] = typeof(AssertModifiersHandler),
            ["AssertMemberSignature"] = typeof(AssertMemberSignatureHandler),
        };

    public HandlerTemplateBase Create(ManifestStep step)
    {
        if (!Map.TryGetValue(step.Operation, out var handlerType))
        {
            throw new NotSupportedException(
                $"Unknown operation '{step.Operation}' (stepId='{step.Id}'). " +
                $"Supported: {string.Join(", ", Map.Keys.OrderBy(x => x))}");
        }

        return (HandlerTemplateBase)ActivatorUtilities.CreateInstance(_sp, handlerType);
    }
}
