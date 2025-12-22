using ReflectionService.Domain.ManifestModel;
using System.Text.Json;

namespace ReflectionService.Domain
{
    public abstract class HandlerTemplateBase<Tres, Targs>
    {
        public HandlerTemplateBase(string operationName) => OperationName = operationName;
        public string OperationName { get; }
        public void Execute(CheckingContext context, ManifestStep step)
        {
            var args = ParseAgrs(step.Args)
                ?? throw new ArgumentException($"Невозможно прочитать агрументы для шага проверки \"{OperationName}\"");

            var res = StartCheck(context, step, args);
            WriteResult(context, res);
        }
        internal protected virtual Targs? ParseAgrs(JsonElement args)
            => args.Deserialize<Targs>(JsonOptions.ManifestArgsConverterOptions);

        internal protected abstract Tres StartCheck(CheckingContext context, ManifestStep step, Targs args);
        internal protected abstract void WriteResult(CheckingContext context, Tres results);
    }
}
