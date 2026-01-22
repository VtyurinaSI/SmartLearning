using Microsoft.Extensions.Logging;
using ReflectionService.Domain.ManifestModel;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("ReflectionService.Domain.Tests")]
namespace ReflectionService.Domain.Steps.FindTypesStep
{
    public class FindTypes : HandlerTemplateBase<FindTypesArgs>
    {
        private readonly ILogger<FindTypes> log;
        public FindTypes(ILogger<FindTypes> _log) : base(nameof(FindTypes))
        {
            log = _log;
        }

        internal protected override TypesResult StartCheck(
            CheckingContext context,
            ManifestStep step,
            FindTypesArgs args)
        {
            var types = context.UserAssembly.GetTypes();
            return new(types);
        }

        internal protected override void WriteResult(CheckingContext context, ManifestStep step, TypesResult results)
        {
            var res = results.Types;
            if (res == null || res.Length == 0)
            {
                context.StepResults.Add(new(
                    step.Id,
                    step.Operation,
                    false,
                    FailureSeverity.Error,
                    "Пустая сборка, типы не найдены"));
                return;
            }

            context.Roles["AllClasses"] = new(RoleValueKind.Types, res);
            context.CachedTypes.AddRange(res);
            context.StepResults.Add(new(step.Id, step.Operation, true));
        }
    }
}
