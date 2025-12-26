using ReflectionService.Domain.ManifestModel;
using System.Reflection;

namespace ReflectionService.Domain
{
    public class CheckingContext
    {
        public CheckingContext(Assembly userAssembly, ManifestTarget target, CheckManifest? manifest = null)
        {
            UserAssembly = userAssembly;
            Target = target;
            Manifest = manifest;
        }

        public Assembly UserAssembly { get; }

        public ManifestTarget Target { get; }

        public CheckManifest? Manifest { get; }

        public Dictionary<string, RoleValue> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<StepResult> StepResults { get; } = new();

        public List<string> Diagnostics { get; } = new();

        public List<Type> CachedTypes { get; } = new();
    }
}
