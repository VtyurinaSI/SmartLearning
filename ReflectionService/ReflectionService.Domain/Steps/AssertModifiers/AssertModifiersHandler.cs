using System.Reflection;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.AssertModifiers;

public sealed class AssertModifiersHandler : HandlerTemplateBase<AssertModifiersArgs>
{
    public AssertModifiersHandler() : base("AssertModifiers") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, AssertModifiersArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана входная роль (InputRole / Input).");

        if (!context.Roles.TryGetValue(input!, out var role) || role.Kind != RoleValueKind.SingleType)
            throw new ArgumentException($"Роль '{input}' не содержит одиночный тип (SingleType).");

        var t = role.Value as Type;
        if (t is null) return new TypesResult(Array.Empty<Type>());

        bool ok = true;
        if (args.Sealed is not null) ok &= (t.IsSealed == args.Sealed.Value);
        if (args.Abstract is not null) ok &= (t.IsAbstract == args.Abstract.Value);
        if (args.StaticClass is not null)
        {
            bool isStaticClass = t.IsAbstract && t.IsSealed && t.IsClass;
            ok &= (isStaticClass == args.StaticClass.Value);
        }
        if (args.Visibility is not null)
        {
            var vis = args.Visibility.Value;
            bool match = vis switch
            {
                TypeVisibility.Any => true,
                TypeVisibility.Public => t.IsPublic || t.IsNestedPublic,
                TypeVisibility.Internal => !t.IsPublic && !t.IsNestedPublic || t.IsNestedAssembly || t.IsNestedFamANDAssem,
                _ => true
            };
            ok &= match;
        }

        if (!ok) return new TypesResult(Array.Empty<Type>());
        return new TypesResult(new[] { t });
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        if (results.Types is null || results.Types.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Тип не соответствует ожидаемым модификаторам"));
            return;
        }

        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static string? GetStringProp(object obj, string propName)
        => obj.GetType().GetProperty(propName)?.GetValue(obj) as string;
}
