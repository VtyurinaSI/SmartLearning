using ReflectionService.Domain.ManifestModel;

namespace ReflectionService.Domain.Steps.PickOne;

public sealed class PickOneHandler : HandlerTemplateBase<PickOneArgs>
{
    public PickOneHandler() : base("PickOne") { }

    internal protected override TypesResult StartCheck(CheckingContext context, ManifestStep step, PickOneArgs args)
    {
        var input = GetStringProp(step, "InputRole") ?? GetStringProp(step, "Input");
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Не указана роль входа (InputRole / Input).");

        if (!context.Roles.TryGetValue(input!, out var role) || role.Kind != RoleValueKind.Types)
            throw new ArgumentException($"Роль '{input}' не содержит типов.");

        var types =
            role.Value as Type[]
            ?? (role.Value as IReadOnlyList<Type>)?.ToArray()
            ?? (role.Value as List<Type>)?.ToArray()
            ?? Array.Empty<Type>();

        if (types.Length == 0)
            return new TypesResult(Array.Empty<Type>());

        Type chosen;
        var strategy = (args.Strategy ?? "only").Trim().ToLowerInvariant();
        switch (strategy)
        {
            case "first":
                chosen = types.OrderBy(t => t.FullName).First();
                break;
            case "only":
            default:
                if (types.Length != 1)
                {
                    if (args.FailIfAmbiguous)
                        return new TypesResult(Array.Empty<Type>());
                    chosen = types.OrderBy(t => t.FullName).First();
                }
                else
                    chosen = types[0];
                break;
        }

        return new TypesResult(new[] { chosen });
    }

    internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
    {
        var res = results.Types;
        if (res == null || res.Length == 0)
        {
            context.StepResults.Add(new(step.Id, step.Operation, false, FailureSeverity.Error, "Не удалось выбрать один тип (ambiguous / not found)"));
            return;
        }

        var output = GetStringProp(step, "OutputRole") ?? GetStringProp(step, "Output");
        if (!string.IsNullOrWhiteSpace(output))
            context.Roles[output!] = new RoleValue(RoleValueKind.SingleType, res[0]);

        context.CachedTypes.AddRange(res);
        context.StepResults.Add(new(step.Id, step.Operation, true));
    }

    private static string? GetStringProp(object obj, string propName)
        => obj.GetType().GetProperty(propName)?.GetValue(obj) as string;
}
