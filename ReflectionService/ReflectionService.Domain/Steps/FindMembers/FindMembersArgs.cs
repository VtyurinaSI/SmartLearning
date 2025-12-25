namespace ReflectionService.Domain.Steps.FindMembers;

[Flags]
public enum MemberKinds
{
    Method = 1,
    Field = 2,
    Property = 4,
    Constructor = 8,
    Any = Method | Field | Property | Constructor
}

public sealed record FindMembersArgs(
    MemberKinds Kinds = MemberKinds.Method,
    TypeVisibility Visibility = TypeVisibility.Any,
    bool? Static = null,
    string? NameRegex = null,
    string? ReturnTypeRegex = null,
    string? MemberTypeRegex = null,
    bool DeclaredOnly = false,
    bool? ExcludeCompilerGenerated = null
);
