using System.Reflection;
using System.Text.RegularExpressions;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.FindMembers;

public sealed class FindMembersHandler : HandlerTemplateBase<FindMembersArgs>
{
    public FindMembersHandler() : base("FindMembers") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, FindMembersArgs args)
    {
        var input = step.InputRole;
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана входная роль (input).");

        if (!context.Roles.TryGetValue(input!, out var role) || role.Kind != RoleValueKind.Types)
            throw new ArgumentException($"Роль '{input}' не содержит типов.");

        var types =
            role.Value as Type[]
            ?? (role.Value as IReadOnlyList<Type>)?.ToArray()
            ?? (role.Value as List<Type>)?.ToArray()
            ?? Array.Empty<Type>();

        var excludeCg = args.ExcludeCompilerGenerated ?? context.Target.ExcludeCompilerGenerated;

        var nameRe = args.NameRegex is null ? null : new Regex(args.NameRegex, RegexOptions.Compiled);
        var returnRe = args.ReturnTypeRegex is null ? null : new Regex(args.ReturnTypeRegex, RegexOptions.Compiled);
        var memberTypeRe = args.MemberTypeRegex is null ? null : new Regex(args.MemberTypeRegex, RegexOptions.Compiled);

        var found = new List<MemberInfo>();

        foreach (var t in types)
        {
            if (t is null) continue;
            if (excludeCg && IsCompilerGenerated(t)) continue;
            if (!MatchVisibility(t, args.Visibility)) continue;

            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            if (args.DeclaredOnly) bf |= BindingFlags.DeclaredOnly;

            if ((args.Kinds & MemberKinds.Method) != 0)
            {
                foreach (var m in t.GetMethods(bf))
                {
                    if (m.IsSpecialName) continue;
                    if (!MatchStatic(m.IsStatic, args.Static)) continue;
                    if (nameRe != null && !nameRe.IsMatch(m.Name)) continue;
                    if (returnRe != null && !returnRe.IsMatch(m.ReturnType?.FullName ?? "")) continue;
                    if (memberTypeRe != null && !memberTypeRe.IsMatch(m.DeclaringType?.FullName ?? "")) continue;
                    found.Add(m);
                }
            }

            if ((args.Kinds & MemberKinds.Property) != 0)
            {
                foreach (var p in t.GetProperties(bf))
                {
                    var accessor = p.GetMethod ?? p.SetMethod;
                    if (accessor is null) continue;
                    if (!MatchStatic(accessor.IsStatic, args.Static)) continue;
                    if (nameRe != null && !nameRe.IsMatch(p.Name)) continue;
                    if (returnRe != null && !returnRe.IsMatch(p.PropertyType?.FullName ?? "")) continue;
                    if (memberTypeRe != null && !memberTypeRe.IsMatch(p.DeclaringType?.FullName ?? "")) continue;
                    found.Add(p);
                }
            }

            if ((args.Kinds & MemberKinds.Field) != 0)
            {
                foreach (var f in t.GetFields(bf))
                {
                    if (!MatchStatic(f.IsStatic, args.Static)) continue;
                    if (nameRe != null && !nameRe.IsMatch(f.Name)) continue;
                    if (returnRe != null && !returnRe.IsMatch(f.FieldType?.FullName ?? "")) continue;
                    if (memberTypeRe != null && !memberTypeRe.IsMatch(f.DeclaringType?.FullName ?? "")) continue;
                    found.Add(f);
                }
            }

            if ((args.Kinds & MemberKinds.Constructor) != 0)
            {
                foreach (var c in t.GetConstructors(bf))
                {
                    if (!MatchStatic(false, args.Static)) continue;
                    if (nameRe != null && !nameRe.IsMatch(c.Name)) continue;
                    if (memberTypeRe != null && !memberTypeRe.IsMatch(c.DeclaringType?.FullName ?? "")) continue;
                    found.Add(c);
                }
            }
        }

        var members = found.ToArray();

        var outputName = !string.IsNullOrWhiteSpace(step.OutputRole) ? step.OutputRole! : step.Id;
        context.Roles[outputName] = new RoleValue(RoleValueKind.Members, members);

        var declaringTypes = members
            .Select(m => m.DeclaringType)
            .Where(t => t is not null)
            .Distinct()
            .ToArray()!;

        return new TypesResult(declaringTypes);
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        var outputName = !string.IsNullOrWhiteSpace(step.OutputRole) ? step.OutputRole! : step.Id;

        if (!context.Roles.TryGetValue(outputName, out var role) || role.Kind != RoleValueKind.Members)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Members output role missing"));
            return;
        }

        var members = role.Value as MemberInfo[] ?? Array.Empty<MemberInfo>();

        if (members.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Члены не найдены"));
            return;
        }

        var types = results.Types;
        if (types is not null && types.Length > 0)
            context.CachedTypes.AddRange(types);

        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static bool MatchStatic(bool memberIsStatic, bool? filter)
        => filter is null || filter.Value == memberIsStatic;

    private static bool MatchVisibility(Type t, TypeVisibility vis)
        => vis switch
        {
            TypeVisibility.Any => true,
            TypeVisibility.Public => t.IsPublic || t.IsNestedPublic,
            TypeVisibility.Internal => !t.IsPublic && !t.IsNestedPublic || t.IsNestedAssembly || t.IsNestedFamANDAssem,
            _ => true
        };

    private static bool IsCompilerGenerated(Type t) =>
        t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
        || (t.FullName?.Contains("<", StringComparison.Ordinal) ?? false);
}
