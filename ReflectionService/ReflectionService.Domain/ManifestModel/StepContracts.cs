namespace ReflectionService.Domain.ManifestModel;

public static class StepContracts
{
    public static readonly IReadOnlyDictionary<string, StepContract> All =
        new Dictionary<string, StepContract>
        {
            ["FindTypes"] = new(
                Operation: "FindTypes",
                ArgsSummary: "kind, visibility, (optional) nameRegex, namespaceRegex, excludeCompilerGenerated",
                Input: RoleValueKind.None,
                Output: RoleValueKind.Types
            ),
            ["PickOne"] = new(
                Operation: "PickOne",
                ArgsSummary: "strategy: only|first, (optional) failIfAmbiguous",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.SingleType
            ),
            ["FindMembers"] = new(
                Operation: "FindMembers",
                ArgsSummary: "memberKind(s), visibility, (optional) static, nameRegex, returns/self, memberType/self, declaredOnly",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.Members
            ),
            ["FindImplementations"] = new(
                Operation: "FindImplementations",
                ArgsSummary: "min (optional), classesRole (optional) - finds classes implementing input abstraction(s)",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.Types
            ),
            ["AssertCount"] = new(
                Operation: "AssertCount",
                ArgsSummary: "equals|min|max",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.None
            ),
            ["AssertExists"] = new(
                Operation: "AssertExists",
                ArgsSummary: "min (optional, default=1)",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.None
            ),
            ["AssertModifiers"] = new(
                Operation: "AssertModifiers",
                ArgsSummary: "sealed|abstract|staticClass|generic (any subset)",
                Input: RoleValueKind.SingleType,
                Output: RoleValueKind.None
            ),
            ["HasDependency"] = new(
                Operation: "HasDependency",
                ArgsSummary: "to: self|role:<name>, kinds: field|property|constructorParam|methodParam, (optional) min",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.Dependencies
            )
            ,
            ["HasCollectionDependency"] = new(
                Operation: "HasCollectionDependency",
                ArgsSummary: "to: self|role:<name>, kinds: field|property|constructorParam|methodParam, (optional) min - looks for collection-of-T dependencies",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.Dependencies
            ),
            ["AssertExists"] = new(
                Operation: "AssertExists",
                ArgsSummary: "min (optional, default=1)",
                Input: RoleValueKind.Types,
                Output: RoleValueKind.None
            ),
            ["AssertModifiers"] = new(
                Operation: "AssertModifiers",
                ArgsSummary: "sealed|abstract|staticClass|visibility",
                Input: RoleValueKind.SingleType,
                Output: RoleValueKind.None
            )
        };
}
