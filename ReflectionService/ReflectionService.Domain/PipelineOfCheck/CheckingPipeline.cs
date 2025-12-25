using ReflectionService.Domain.ManifestModel;
using System.Reflection;

namespace ReflectionService.Domain.PipelineOfCheck
{
    public class CheckingPipeline
    {
        private List<HandlerTemplateBase> pipeline = [];

        public void SetPipeline(CheckManifest rules)
        {

        }

        public void ExecutePipleine(Assembly userAssembly, ManifestTarget target)
        {

        }
    }
}
