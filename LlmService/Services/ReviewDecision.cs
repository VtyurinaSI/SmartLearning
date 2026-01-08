namespace LlmService;

public sealed record ReviewDecision(bool Passed, string Explanation, double? Confidence, string RawAnswer);
