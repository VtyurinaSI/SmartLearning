using ReflectionService.Domain.ManifestModel;
using System.Reflection;

namespace ReflectionService.Domain
{
    public class CheckingContext
    {
        public CheckingContext(Assembly userAssembly, ManifestTarget target)
        {
            UserAssembly = userAssembly;
            Target = target; 
        }

        public Assembly UserAssembly { get; }

        public ManifestTarget Target { get; }

        public Dictionary<string, RoleValue> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<StepResult> StepResults { get; } = [];

        public List<string> Diagnostics { get; } = [];
        public List<Type> CachedTypes { get; } = [];
    }
}
