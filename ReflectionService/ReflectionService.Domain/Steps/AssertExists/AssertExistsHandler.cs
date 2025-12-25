using ReflectionService.Domain.ManifestModel;
using System.Reflection;
using System.Linq;

namespace ReflectionService.Domain.Steps.AssertExists;

public sealed class AssertExistsHandler : HandlerTemplateBase<AssertExistsArgs>
{
    public AssertExistsHandler() : base("AssertExists") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, AssertExistsArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана входная роль (InputRole / Input).");

        if (!context.Roles.TryGetValue(input!, out var role))
            throw new ArgumentException($"Роль '{input}' не найдена.");

        if (role.Kind == RoleValueKind.Types)
        {
            var types = role.Value as Type[] ?? (role.Value as IReadOnlyList<Type>)?.ToArray() ?? Array.Empty<Type>();
            return new TypesResult(types);
        }
        return new TypesResult(Array.Empty<Type>());
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        int min = GetIntProp(step, nameof(AssertExistsArgs.Min)) ?? 1;
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (!context.Roles.TryGetValue(input!, out var role))
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, $"Роль '{input}' не найдена"));
            return;
        }

        int count = role.Kind switch
        {
            RoleValueKind.Types => (role.Value as Type[])?.Length ?? (role.Value as IReadOnlyList<Type>)?.Count ?? 0,
            RoleValueKind.Members => (role.Value as MemberInfo[])?.Length ?? (role.Value as IReadOnlyList<MemberInfo>)?.Count ?? 0,
            RoleValueKind.SingleType => role.Value is Type ? 1 : 0,
            _ => 0
        };

        if (count < min)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, $"Найдено элементов: {count}. Ожидалось минимум: {min}."));
            return;
        }

        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static int? GetIntProp(object obj, string propName)
    {
        var pi = obj.GetType().GetProperty(propName);
        if (pi is null) return null;
        var val = pi.GetValue(obj);
        if (val is null) return null;
        return val is int i ? i : null;
    }

    private static string? GetStringProp(object obj, string propName)
        => obj.GetType().GetProperty(propName)?.GetValue(obj) as string;
}
