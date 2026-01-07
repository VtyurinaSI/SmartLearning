namespace PatternService;

public sealed record TaskMeta(
    long TaskId,
    string TaskTitle,
    string PatternKey,
    string PatternTitle,
    int Version
);
