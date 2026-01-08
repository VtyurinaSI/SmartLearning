using System.Text.Json;

namespace LlmService;

public sealed class ReviewResultParser
{
    private sealed record ReviewResult(bool passed, string? explanation, double? confidence);

    public ReviewDecision Parse(string answer)
    {
        ReviewResult? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<ReviewResult>(answer, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
        }

        bool passed;
        string explanation = answer;
        double? confidence = null;

        if (parsed is not null)
        {
            passed = parsed.passed;
            explanation = parsed.explanation ?? answer;
            confidence = parsed.confidence;
        }
        else
        {
            var lower = answer.ToLowerInvariant();
            if (lower.Contains("не прой") || lower.Contains("failed") || lower.Contains("не пройден"))
                passed = false;
            else if (lower.Contains("пройден") || lower.Contains("passed") || lower.Contains("успешно"))
                passed = true;
            else
                passed = false;
        }

        return new ReviewDecision(passed, explanation, confidence, answer);
    }
}
