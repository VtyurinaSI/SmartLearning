using Microsoft.Extensions.Logging;
using ReflectionService.Domain.ManifestModel;
using System.Text.Json;

namespace ReflectionService.Domain.Steps.FindTypesStep
{
    public class FindTypes : HandlerTemplateBase<ResultOfFindTypes, FindTypesArgs>
    {
        private readonly ILogger<FindTypes> log;
        public FindTypes(ILogger<FindTypes> _log) : base(nameof(FindTypes)) { 
            log = _log;
        }

        private protected override ResultOfFindTypes StartCheck(CheckingContext context, ManifestStep step, FindTypesArgs args)
        {
            throw new NotImplementedException();
        }

        private protected override void WriteResult(CheckingContext context, ResultOfFindTypes results)
        {
            throw new NotImplementedException();
        }
    }
}
