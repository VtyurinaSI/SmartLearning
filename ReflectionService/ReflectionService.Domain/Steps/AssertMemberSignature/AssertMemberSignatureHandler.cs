using System.Reflection;
using System.Text.RegularExpressions;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.AssertMemberSignature;

public sealed class AssertMemberSignatureHandler : HandlerTemplateBase<AssertMemberSignatureArgs>
{
    public AssertMemberSignatureHandler() : base("AssertMemberSignature") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, AssertMemberSignatureArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана входная роль (InputRole / Input).");

        if (!context.Roles.TryGetValue(input!, out var role) || role.Kind != RoleValueKind.Members)
            throw new ArgumentException($"Роль '{input}' не содержит членов (Members).");

        var members =
            role.Value as MemberInfo[]
            ?? (role.Value as IReadOnlyList<MemberInfo>)?.ToArray()
            ?? (role.Value as List<MemberInfo>)?.ToArray()
            ?? Array.Empty<MemberInfo>();

        var nameRe = args.NameRegex is null ? null : new Regex(args.NameRegex, RegexOptions.Compiled);
        var returnRe = args.ReturnTypeRegex is null ? null : new Regex(args.ReturnTypeRegex, RegexOptions.Compiled);
        var paramRes = args.ParamTypeRegexes?.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

        var matched = new List<MemberInfo>();
        foreach (var m in members)
        {
            if (m is null) continue;

            if (!MemberVisibilityMatches(m, args.Visibility)) continue;
            if (!MatchStatic(MemberIsStatic(m), args.Static)) continue;
            if (nameRe != null && !nameRe.IsMatch(m.Name)) continue;

            Type? retType = m switch
            {
                MethodInfo mi => mi.ReturnType,
                PropertyInfo pi => pi.PropertyType,
                FieldInfo fi => fi.FieldType,
                ConstructorInfo => null,
                _ => null
            };
            if (returnRe != null)
            {
                if (retType is null || !returnRe.IsMatch(retType.FullName ?? "")) continue;
            }

            if (paramRes is not null && paramRes.Length > 0)
            {
                ParameterInfo[] ps = m switch
                {
                    MethodBase mb => mb.GetParameters(),
                    _ => Array.Empty<ParameterInfo>()
                };
                if (ps.Length != paramRes.Length)
                    continue;

                bool ok = true;
                for (int i = 0; i < paramRes.Length; i++)
                {
                    var pTypeName = ps[i].ParameterType.FullName ?? "";
                    if (!paramRes[i].IsMatch(pTypeName)) { ok = false; break; }
                }
                if (!ok) continue;
            }

            matched.Add(m);
        }

        var declTypes = matched.Select(m => m.DeclaringType).Where(t => t is not null).Distinct().ToArray()!;
        return new TypesResult(declTypes);
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        int min = GetIntProp(step, nameof(AssertMemberSignatureArgs.MinMatches)) ?? 1;
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        var members =
            context.Roles.TryGetValue(input!, out var role) && role.Kind == RoleValueKind.Members
            ? role.Value as MemberInfo[] ?? (role.Value as IReadOnlyList<MemberInfo>)?.ToArray() ?? Array.Empty<MemberInfo>()
            : Array.Empty<MemberInfo>();

        if (results.Types is null || results.Types.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, $"Члены с заданной сигнатурой не найдены"));
            return;
        }

        var count = members.Count(m => m.DeclaringType is not null && results.Types.Contains(m.DeclaringType));
        int expectedMin = GetIntProp(step, nameof(AssertMemberSignatureArgs.MinMatches)) ?? 1;
        if (count < expectedMin)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, $"Найдено членов: {count}. Ожидалось минимум: {expectedMin}."));
            return;
        }

        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static bool MemberIsStatic(MemberInfo m) => m switch
    {
        MethodBase mb => mb.IsStatic,
        FieldInfo fi => fi.IsStatic,
        PropertyInfo pi => (pi.GetMethod ?? pi.SetMethod)?.IsStatic ?? false,
        _ => false
    };

    private static bool MatchStatic(bool memberIsStatic, bool? filter)
        => filter is null || filter.Value == memberIsStatic;

    private static bool MemberVisibilityMatches(MemberInfo m, TypeVisibility vis)
    {
        MethodBase? mb = m as MethodBase;
        if (mb is not null)
        {
            if (vis == TypeVisibility.Any) return true;
            if (vis == TypeVisibility.Public) return mb.IsPublic;
            if (vis == TypeVisibility.Internal) return mb.IsAssembly || mb.IsFamilyAndAssembly;
        }
        return true;
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
