using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.Steps.FindTypesStep;
using ReflectionService.Domain.Steps.FindInterface;
using ReflectionService.Domain.Steps.FindImplementations;
using ReflectionService.Domain.Steps.FindCtor;
using ReflectionService.Domain.Steps.CountTypes;

namespace ReflectionService.Domain.Strategies;

public sealed class CodeCheckingStrategy
{
    private readonly FindTypes findTypes;
    private readonly FindInterfacesHandler findInterfaces;
    private readonly FindImplementationsHandler findImplementations;
    private readonly FindCtorConsumersHandler findCtorConsumers;
    private readonly CountTypesHandler countTypes;

    public CodeCheckingStrategy()
    {
        findTypes = new FindTypes(NullLogger<FindTypes>.Instance);
        findInterfaces = new FindInterfacesHandler();
        findImplementations = new FindImplementationsHandler();
        findCtorConsumers = new FindCtorConsumersHandler();
        countTypes = new CountTypesHandler();
    }
    public CheckingContext Run(Assembly userAssembly, ManifestTarget target, string interfaceNameRegex = ".*", int minImplementations = 2)
    {
        var context = new CheckingContext(userAssembly, target);
         
        var ftArgsJson = JsonDocument.Parse($@"{{""kind"": ""Any"", ""visibility"": ""Any""}}").RootElement;
        var stepFindTypes = new ManifestStep { Id = "step-findtypes", Operation = "FindTypes", Args = ftArgsJson, OutputRole = "AllClasses" };
        findTypes.Execute(context, stepFindTypes);

        var fiArgsJson = JsonDocument.Parse($@"{{""visibility"": ""Any"", ""nameRegex"": ""{EscapeForJson(interfaceNameRegex)}""}}").RootElement;
        var stepFindInterfaces = new ManifestStep { Id = "step-findinterfaces", Operation = "FindInterfaces", Args = fiArgsJson, OutputRole = "Interfaces" };
        findInterfaces.Execute(context, stepFindInterfaces);

        if (!context.Roles.TryGetValue("Interfaces", out var interfacesRole) || interfacesRole.Kind != RoleValueKind.Types)
        {
            context.StepResults.Add(new("step-findimplementations", "FindImplementations", false, FailureSeverity.Error, "Роль Interfaces не найдена или пуста"));
            return context;
        }

        var interfaces = (interfacesRole.Value as Type[]) ?? Array.Empty<Type>();
        if (interfaces.Length == 0)
        {
            context.StepResults.Add(new("step-findimplementations", "FindImplementations", false, FailureSeverity.Error, "Интерфейсы не найдены"));
            return context;
        }

       var ctArgsJson = JsonDocument.Parse($@"{{""kind"": ""Class"", ""visibility"": ""Any"", ""min"": {minImplementations} }}").RootElement;
        var stepCountImpls = new ManifestStep
        {
            Id = "step-countimpls",
            Operation = "CountTypes",
            Args = ctArgsJson,
            InputRole = "Interfaces",
            OutputRole = "Implementations"
        };
        countTypes.Execute(context, stepCountImpls);

        if (!context.Roles.TryGetValue("Implementations", out var implRole) || implRole.Kind != RoleValueKind.Types)
        {
            context.StepResults.Add(new("step-assert-count", "AssertCount", false, FailureSeverity.Error, "Роль Implementations не найдена"));
            return context;
        }

        var impls = (implRole.Value as Type[]) ?? Array.Empty<Type>();
        if (impls.Length < minImplementations)
        {
            context.StepResults.Add(new("step-assert-count", "AssertCount", false, FailureSeverity.Error, $"Найдено реализаций: {impls.Length}. Ожидалось минимум {minImplementations}."));
            return context;
        }
        context.StepResults.Add(new("step-assert-count", "AssertCount", true));

        var consumersArgsJson = JsonDocument.Parse($@"{{""visibility"": ""Any""}}").RootElement;
         var stepFindConsumers = new ManifestStep { Id = "step-findconsumers", Operation = "FindCtorConsumers", Args = consumersArgsJson, InputRole = "Interfaces", OutputRole = "Consumers" };
        findCtorConsumers.Execute(context, stepFindConsumers);

        return context;
    }

    private static string EscapeForJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}