using System.Text;

namespace LlmService;

public sealed class ReviewResponseFormatter
{
    public string Format(ReviewDecision decision)
    {
        var finalText = new StringBuilder();
        finalText.AppendLine($"status: {(decision.Passed ? "passed" : "failed")}");
        if (decision.Confidence is not null)
            finalText.AppendLine($"confidence: {decision.Confidence:0.##}");
        finalText.AppendLine();
        finalText.AppendLine("explanation:");
        finalText.AppendLine(decision.Explanation);
        finalText.AppendLine();
        finalText.AppendLine("raw_llm_response:");
        finalText.AppendLine(decision.RawAnswer);
        return finalText.ToString();
    }
}
