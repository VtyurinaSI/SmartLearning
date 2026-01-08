namespace LlmService;

public sealed record ChatMessage(string role, string content);
public sealed record ChatRequest(string model, ChatMessage[] messages);
public sealed record Choice(int index, ChatMessage message);
public sealed record ChatResponse(Choice[] choices);
