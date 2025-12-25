using ReflectionService.Domain.ManifestModel;
using System.Text.Json;

namespace ReflectionService.Domain
{
    public abstract class HandlerTemplateBase
    {
        protected HandlerTemplateBase(string operationName) => OperationName = operationName;
        public string OperationName { get; }

        public abstract void Execute(CheckingContext context, ManifestStep step);
    }

    public abstract class HandlerTemplateBase<Targs> : HandlerTemplateBase
    {
        public HandlerTemplateBase(string operationName) : base(operationName) { }

        public override void Execute(CheckingContext context, ManifestStep step)
        {
            var args = ParseArgs(step.Args)
                ?? throw new ArgumentException($"Невозможно прочитать агрументы для шага проверки \"{OperationName}\"");

            var res = StartCheck(context, step, args);
            WriteResult(context, step, res);
        }

        internal protected virtual Targs? ParseArgs(JsonElement args)
            => args.Deserialize<Targs>(JsonOptions.ManifestArgsConverterOptions);

        internal protected abstract TypesResult StartCheck(CheckingContext context, ManifestStep step, Targs args);
        internal protected abstract void WriteResult(CheckingContext context, ManifestStep step, TypesResult results);
    }
}
