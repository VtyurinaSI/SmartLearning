namespace ReflectionService.Domain.ManifestModel;

public sealed record StepContract(
    string Operation,
    string ArgsSummary,
    RoleValueKind Input,
    RoleValueKind Output
);
