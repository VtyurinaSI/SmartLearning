using System.Reflection;
using System.Text.RegularExpressions;
using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.FindInterface;

public sealed class FindInterfacesHandler : HandlerTemplateBase<FindInterfacesArgs>
{
    public FindInterfacesHandler() : base("FindInterfaces") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, FindInterfacesArgs args)
    {
        var excludeCg = args.ExcludeCompilerGenerated ?? context.Target.ExcludeCompilerGenerated;

        var types = GetAllTypesSafe(context.UserAssembly)
            .Where(t => t.IsInterface)
            .Where(t => MatchVisibility(t, args.Visibility))
            .Where(t => !excludeCg || !IsCompilerGenerated(t))
            .Where(t => args.NamespaceRegex is null || t.Namespace is not null && Regex.IsMatch(t.Namespace, args.NamespaceRegex))
            .Where(t => args.NameRegex is null || Regex.IsMatch(t.Name, args.NameRegex))
            .OrderBy(t => t.FullName)
            .ToArray();

        var output = GetStringProp(step, "OutputRole") ?? GetStringProp(step, "Output");
        if (!string.IsNullOrWhiteSpace(output))
            context.Roles[output] = new RoleValue(RoleValueKind.Types, types);

        context.StepResults.Add(new StepResult(step.Id, step.Operation, true));
        return new TypesResult(types);
    }

    internal protected override void WriteResult(CheckingContext context, TypesResult results) { }

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
