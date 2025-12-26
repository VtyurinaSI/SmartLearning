using ReflectionService.Domain.StepFactory;
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

namespace ReflectionService.Application;

public static class ReflectionStepsServiceCollectionExtensions
{
    public static IServiceCollection AddReflectionStepHandlers(this IServiceCollection services)
    {
        
        services.AddTransient<FindTypes>();
        services.AddTransient<CountTypesHandler>();
        services.AddTransient<FindInterfacesHandler>();
        services.AddTransient<FindCtorConsumersHandler>();
        services.AddTransient<FindImplementationsHandler>();
        services.AddTransient<FindMembersHandler>();
        services.AddTransient<PickOneHandler>();
        services.AddTransient<HasDependencyHandler>();
        services.AddTransient<HasCollectionDependencyHandler>();
        services.AddTransient<AssertExistsHandler>();
        services.AddTransient<AssertModifiersHandler>();
        services.AddTransient<AssertMemberSignatureHandler>();

        
        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<FindTypes>("FindTypes", sp.GetRequiredService<FindTypes>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<CountTypesHandler>("CountTypes", sp.GetRequiredService<CountTypesHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<FindInterfacesHandler>("FindInterfaces", sp.GetRequiredService<FindInterfacesHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<FindCtorConsumersHandler>("FindCtorConsumers", sp.GetRequiredService<FindCtorConsumersHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<FindImplementationsHandler>("FindImplementations", sp.GetRequiredService<FindImplementationsHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<FindMembersHandler>("FindMembers", sp.GetRequiredService<FindMembersHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<PickOneHandler>("PickOne", sp.GetRequiredService<PickOneHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<HasDependencyHandler>("HasDependency", sp.GetRequiredService<HasDependencyHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<HasCollectionDependencyHandler>("HasCollectionDependency", sp.GetRequiredService<HasCollectionDependencyHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<AssertExistsHandler>("AssertExists", sp.GetRequiredService<AssertExistsHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<AssertModifiersHandler>("AssertModifiers", sp.GetRequiredService<AssertModifiersHandler>));

        services.AddSingleton<IStepHandlerRegistration>(sp =>
            new StepHandlerRegistration<AssertMemberSignatureHandler>("AssertMemberSignature", sp.GetRequiredService<AssertMemberSignatureHandler>));

        
        services.AddSingleton<IStepHandlerFactory, StepHandlerFactory>();

        return services;
    }
}
