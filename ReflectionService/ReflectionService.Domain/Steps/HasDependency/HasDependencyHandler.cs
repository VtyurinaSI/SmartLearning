using System.Reflection;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.HasDependency;

public sealed class HasDependencyHandler : HandlerTemplateBase<HasDependencyArgs>
{
    public HasDependencyHandler() : base("HasDependency") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, HasDependencyArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана входная роль (InputRole / Input).");

        if (!context.Roles.TryGetValue(input!, out var role) || role.Kind != RoleValueKind.Types)
            throw new ArgumentException($"Роль '{input}' не содержит типов.");

        var targets =
            role.Value as Type[]
            ?? (role.Value as IReadOnlyList<Type>)?.ToArray()
            ?? (role.Value as List<Type>)?.ToArray()
            ?? Array.Empty<Type>();

        var excludeCg = args.ExcludeCompilerGenerated ?? context.Target.ExcludeCompilerGenerated;

        var allTypes = GetAllTypesSafe(context.UserAssembly);

        var matched = new List<Type>();
        foreach (var t in allTypes)
        {
            if (t is null) continue;
            if (!MatchVisibility(t, args.Visibility)) continue;
            if (excludeCg && IsCompilerGenerated(t)) continue;

            if ((args.Kinds & DependencyKinds.Field) != 0)
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (targets.Any(tr => tr.IsAssignableFrom(f.FieldType))) { matched.Add(t); break; }
                }
                if (matched.Contains(t)) continue;
            }

            if ((args.Kinds & DependencyKinds.Property) != 0)
            {
                foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (targets.Any(tr => tr.IsAssignableFrom(p.PropertyType))) { matched.Add(t); break; }
                }
                if (matched.Contains(t)) continue;
            }

            if ((args.Kinds & DependencyKinds.ConstructorParam) != 0)
            {
                foreach (var c in t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var param in c.GetParameters())
                    {
                        if (targets.Any(tr => tr.IsAssignableFrom(param.ParameterType))) { matched.Add(t); break; }
                    }
                    if (matched.Contains(t)) break;
                }
                if (matched.Contains(t)) continue;
            }

            if ((args.Kinds & DependencyKinds.MethodParam) != 0)
            {
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var param in m.GetParameters())
                    {
                        if (targets.Any(tr => tr.IsAssignableFrom(param.ParameterType))) { matched.Add(t); break; }
                    }
                    if (matched.Contains(t)) break;
                }
                if (matched.Contains(t)) continue;
            }
        }

        var res = matched.Distinct().OrderBy(x => x.FullName).ToArray();
        return new TypesResult(res);
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        var res = results.Types;
        if (res == null || res.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Зависимости не найдены"));
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
