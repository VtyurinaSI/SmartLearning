using System.Reflection;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.FindCtor;

public sealed class FindCtorConsumersHandler : HandlerTemplateBase<FindCtorConsumersArgs>
{
    public FindCtorConsumersHandler() : base("FindCtorConsumers") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, FindCtorConsumersArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не задан входной параметр.");

        if (!context.Roles.TryGetValue(input, out var role) || role.Kind != RoleValueKind.Types)
            throw new ArgumentException($"Роль '{input}' не содержит типов.");

        var abstractions =
            role.Value as Type[]
            ?? (role.Value as IReadOnlyList<Type>)?.ToArray()
            ?? (role.Value as List<Type>)?.ToArray()
            ?? Array.Empty<Type>();

        var excludeCg = args.ExcludeCompilerGenerated ?? context.Target.ExcludeCompilerGenerated;

        var consumers = GetAllTypesSafe(context.UserAssembly)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => MatchVisibility(t, args.Visibility))
            .Where(t => !excludeCg || !IsCompilerGenerated(t))
            .Where(t => HasCtorParamOfAny(t, abstractions))
            .OrderBy(t => t.FullName)
            .ToArray();

        return new TypesResult(consumers);
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        var res = results.Types;
        if (res == null || res.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Классы-потребители не найдены"));
            return;
        }

        var outputRole = GetStringProp(step, "OutputRole") ?? GetStringProp(step, "Output");
        if (!string.IsNullOrWhiteSpace(outputRole))
            context.Roles[outputRole!] = new RoleValue(RoleValueKind.Types, res);

        context.CachedTypes.AddRange(res);
        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static bool HasCtorParamOfAny(Type t, Type[] abstractions)
    {
        var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var ctor in ctors)
        foreach (var p in ctor.GetParameters())
            if (abstractions.Any(a => a.IsAssignableFrom(p.ParameterType)))
                return true;

        return false;
    }

    private static IEnumerable<Type> GetAllTypesSafe(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!.Cast<Type>(); }
    }

    private static bool MatchVisibility(Type t, TypeVisibility vis) => vis switch
    {
        TypeVisibility.Any => true,
        TypeVisibility.Public => t.IsPublic || t.IsNestedPublic,
        TypeVisibility.Internal => !t.IsPublic && !t.IsNestedPublic || t.IsNestedAssembly || t.IsNestedFamANDAssem,
        _ => true
    };

    private static bool IsCompilerGenerated(Type t) =>
        t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
        || (t.FullName?.Contains("<", StringComparison.Ordinal) ?? false);

    private static string? GetStringProp(object obj, string propName)
        => obj.GetType().GetProperty(propName)?.GetValue(obj) as string;
}
