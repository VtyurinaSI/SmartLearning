using System.Reflection;
using System.Text.RegularExpressions;
using ReflectionService.Domain.ManifestModel;
using ReflectionService.Domain.Steps.FindTypesStep;

namespace ReflectionService.Domain.Steps.CountTypes;

public sealed class CountTypesHandler : HandlerTemplateBase<CountTypesArgs>
{
    public CountTypesHandler() : base(nameof(CountTypes)) { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, CountTypesArgs args)
    {
        var excludeCg = args.ExcludeCompilerGenerated ?? context.Target.ExcludeCompilerGenerated;

        // Попытка получить входную роль (InputRole / Input)
        var inputRoleName = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        Type[]? roleTypes = null;
        if (!string.IsNullOrWhiteSpace(inputRoleName))
        {
            if (!context.Roles.TryGetValue(inputRoleName!, out var role) || role.Kind != RoleValueKind.Types)
                throw new ArgumentException($"Роль '{inputRoleName}' не содержит типов.");

            roleTypes =
                role.Value as Type[]
                ?? (role.Value as IReadOnlyList<Type>)?.ToArray()
                ?? (role.Value as List<Type>)?.ToArray()
                ?? Array.Empty<Type>();
        }

        var allTypes = GetAllTypesSafe(context.UserAssembly);

        IEnumerable<Type> source;
        if (roleTypes is not null && roleTypes.Length > 0 && roleTypes.All(rt => rt.IsInterface || rt.IsAbstract))
            source = allTypes.Where(t => roleTypes.Any(abs => abs.IsAssignableFrom(t)));
        else if (roleTypes is not null && roleTypes.Length > 0)
            source = roleTypes;
        else
            source = allTypes;

        var types = source
            .Where(t => KindMatches(t, args.Kind))
            .Where(t => MatchVisibility(t, args.Visibility))
            .Where(t => !excludeCg || !IsCompilerGenerated(t))
            .Where(t => args.NamespaceRegex is null || t.Namespace is not null && Regex.IsMatch(t.Namespace, args.NamespaceRegex))
            .Where(t => args.NameRegex is null || Regex.IsMatch(t.Name, args.NameRegex))
            .OrderBy(t => t.FullName)
            .ToArray();

        return new TypesResult(types);
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        var res = results.Types ?? Array.Empty<Type>();
        var count = res.Length;

        bool ok;
        string? expectation;

        int? exact = GetIntProp(step, nameof(CountTypesArgs.Exact));
        int? min = GetIntProp(step, nameof(CountTypesArgs.Min));
        int? max = GetIntProp(step, nameof(CountTypesArgs.Max));

        if (exact is not null)
        {
            ok = count == exact;
            expectation = $"ожидалось ровно {exact}";
        }
        else if (min is not null || max is not null)
        {
            ok = (min is null || count >= min) && (max is null || count <= max);
            if (min is not null && max is not null) expectation = $"ожидалось от {min} до {max}";
            else if (min is not null) expectation = $"ожидалось не меньше {min}";
            else expectation = $"ожидалось не больше {max}";
        }
        else
        {
            ok = count > 0;
            expectation = "ожидалось наличие по крайней мере одного типа";
        }

        if (!ok)
        {
            var message = $"Найдено типов: {count}. {expectation}.";
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, message));
            return;
        }

        var output = GetStringProp(step, "OutputRole") ?? GetStringProp(step, "Output");
        if (!string.IsNullOrWhiteSpace(output))
            context.Roles[output!] = new RoleValue(RoleValueKind.Types, res);

        context.CachedTypes.AddRange(res);
        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static IEnumerable<Type> GetAllTypesSafe(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!.Cast<Type>(); }
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

    private static bool IsCompilerGenerated(Type t) =>
        t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
        || (t.FullName?.Contains("<", StringComparison.Ordinal) ?? false);

    private static bool KindMatches(Type t, TypeKind kind)
        => kind switch
        {
            TypeKind.Class => t.IsClass && !t.IsAbstract,
            TypeKind.Interface => t.IsInterface,
            TypeKind.Abstract => t.IsClass && t.IsAbstract,
            TypeKind.Any => true,
            _ => true
        };
    private static bool MatchVisibility(Type t, TypeVisibility vis)
        => vis switch
        {
            TypeVisibility.Any => true,
            TypeVisibility.Public => t.IsPublic || t.IsNestedPublic,
            TypeVisibility.Internal => !t.IsPublic && !t.IsNestedPublic || t.IsNestedAssembly || t.IsNestedFamANDAssem,
            _ => true
        };
}
