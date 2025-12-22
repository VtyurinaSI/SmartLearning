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
        private string stepId = null!;
        private string operation = null!;
        internal protected override TypesResult StartCheck(
            CheckingContext UserAssembly,
            ManifestStep step,
            FindTypesArgs args)
        {
            stepId = step.Id;
            operation = step.Operation;
            var types = UserAssembly.UserAssembly.GetTypes();

            return new(types);
        }

        internal protected override void WriteResult(CheckingContext context, TypesResult results)
        {
            var res = results.Types;
            if (res == null)
            {
                context.StepResults.Add(new(
                    stepId,
                    operation,
                    false,
                    FailureSeverity.Error,
                    "Пустая сборка, типы не найдены"));
                return;
            }
            context.Roles["AllClasses"] = new(RoleValueKind.Types, res);
            context.StepResults.Add(new(stepId, operation, true));
        }
    }
}
