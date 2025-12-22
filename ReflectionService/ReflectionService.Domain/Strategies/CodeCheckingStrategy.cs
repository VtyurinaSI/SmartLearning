using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.Steps.FindTypesStep;
using ReflectionService.Domain.Steps.FindInterface;
using ReflectionService.Domain.Steps.FindImplementations;
using ReflectionService.Domain.Steps.FindCtor;

namespace ReflectionService.Domain.Strategies;

public sealed class CodeCheckingStrategy
{
    private readonly FindTypes findTypes;
    private readonly FindInterfacesHandler findInterfaces;
    private readonly FindImplementationsHandler findImplementations;
    private readonly FindCtorConsumersHandler findCtorConsumers;

    public CodeCheckingStrategy()
    {
        findTypes = new FindTypes(NullLogger<FindTypes>.Instance);
        findInterfaces = new FindInterfacesHandler();
        findImplementations = new FindImplementationsHandler();
        findCtorConsumers = new FindCtorConsumersHandler();
    }

    /// <summary>
    /// Выполнить последовательную проверку:
    /// 1) все типы,
    /// 2) интерфейсы (опц. фильтр по имени),
    /// 3) реализации интерфейсов,
    /// 4) проверить, что реализаций >= minImplementations (по умолчанию 2),
    /// 5) найти классы, принимающие интерфейс в конструкторе.
    /// Возвращает CheckingContext с Roles, CachedTypes и StepResults.
    /// </summary>
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

        var implArgsJson = JsonDocument.Parse($@"{{""visibility"": ""Any"", ""includeAbstract"": false}}").RootElement;
        var stepFindImpl = new ManifestStep { Id = "step-findimpls", Operation = "FindImplementations", Args = implArgsJson, InputRole = "Interfaces", OutputRole = "Implementations" };
        findImplementations.Execute(context, stepFindImpl);

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
        var stepFindConsumers = new ManifestStep { Id = "step-findconsumers", Operation = "FindCtorConsumers", Args = consumersArgsJson, InputRole = "Implementations", OutputRole = "Consumers" };
        findCtorConsumers.Execute(context, stepFindConsumers);

        return context;
    }

    private static string EscapeForJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}