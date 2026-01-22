using System.Text;

namespace ReflectionService.Domain.Reporting;

public sealed class CheckingReportBuilder : ICheckingReportBuilder
{
    public string Build(CheckingContext context)
    {
        var sb = new StringBuilder();

        var manifest = context.Manifest;
        var pattern = manifest?.Pattern ?? "unknown";

        sb.AppendLine(pattern);

        if (manifest is null)
        {
            AppendResultsWithoutManifest(sb, context);
            return sb.ToString().TrimEnd();
        }

        var resultsById = context.StepResults.ToDictionary(x => x.StepId, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < manifest.Steps.Length; i++)
        {
            var step = manifest.Steps[i];
            resultsById.TryGetValue(step.Id, out var result);

            var title = step.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = step.Operation;

            var passed = result?.Passed ?? false;
            var status = passed ? "пройден" : "не пройден";

            sb.Append(i + 1);
            sb.Append(".  ");
            sb.Append(title);
            sb.Append(": ");
            sb.AppendLine(status);

            if (!passed)
            {
                var severity = result?.Severity ?? (FailureSeverity)step.OnFail.Severity;
                var message = result?.Message ?? step.OnFail.Message;

                if (!string.IsNullOrWhiteSpace(step.Description))
                    sb.AppendLine(step.Description);

                if (!string.IsNullOrWhiteSpace(message))
                {
                    sb.Append("- ");
                    sb.Append(severity.ToString().ToLowerInvariant());
                    sb.Append(": ");
                    sb.AppendLine(message);
                }

                if (!string.IsNullOrWhiteSpace(result?.Details))
                    sb.AppendLine(result.Details);
            }
        }

        if (context.Diagnostics.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("diagnostics");
            foreach (var d in context.Diagnostics)
                sb.AppendLine("- " + d);
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendResultsWithoutManifest(StringBuilder sb, CheckingContext context)
    {
        for (var i = 0; i < context.StepResults.Count; i++)
        {
            var r = context.StepResults[i];
            var status = r.Passed ? "пройден" : "не пройден";

            sb.Append(i + 1);
            sb.Append(".  ");
            sb.Append(r.Operation);
            sb.Append(": ");
            sb.AppendLine(status);

            if (!r.Passed)
            {
                var severity = r.Severity?.ToString().ToLowerInvariant() ?? "error";
                var message = r.Message ?? "step failed";

                sb.Append("- ");
                sb.Append(severity);
                sb.Append(": ");
                sb.AppendLine(message);

                if (!string.IsNullOrWhiteSpace(r.Details))
                    sb.AppendLine(r.Details);
            }
        }
    }
}
