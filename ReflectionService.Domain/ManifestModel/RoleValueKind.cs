namespace ReflectionService.Domain.ManifestModel;

/// <summary>
/// What kind of value a step stores into a role.
/// Mostly for documentation/validation.
/// </summary>
public enum RoleValueKind
{
    None,
    Types,
    SingleType,
    Members,
    Dependencies
}
