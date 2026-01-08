using Microsoft.Extensions.Options;
using System.Text;

namespace LlmService;

public sealed class ReviewPromptBuilder
{
    private readonly ReviewPromptOptions _options;

    public ReviewPromptBuilder(IOptions<ReviewPromptOptions> options)
    {
        _options = options.Value;
    }

    public string Build(
        string rootDir,
        IReadOnlyList<string> files,
        string projectStructure,
        string patternName)
    {
        var maxChars = GetEnvInt("LLM_PROMPT_MAX_CHARS", 120_000);
        var perFileMaxChars = GetEnvInt("LLM_PROMPT_PER_FILE_MAX_CHARS", 20_000);

        var template = _options.Template ?? string.Empty;
        var header = template.Replace("{patternName}", patternName);

        var sb = new StringBuilder(Math.Min(maxChars, 64_000));

        sb.AppendLine(header);
        sb.AppendLine(projectStructure);
        sb.AppendLine();

        foreach (var f in files)
        {
            if (sb.Length >= maxChars)
                break;

            var rel = Path.GetRelativePath(rootDir, f).Replace('\\', '/');
            sb.AppendLine($"--- FILE: {rel} ---");

            string text;
            try { text = File.ReadAllText(f, Encoding.UTF8); }
            catch { text = File.ReadAllText(f); }

            if (text.Length > perFileMaxChars)
                text = text[..perFileMaxChars] + "\n\n[TRUNCATED]\n";

            var reserve = 256;

            if (sb.Length + text.Length + reserve > maxChars)
            {
                var space = Math.Max(0, maxChars - sb.Length - reserve);
                if (space > 0)
                    sb.AppendLine(text[..Math.Min(space, text.Length)]);
                sb.AppendLine("[TRUNCATED BY TOTAL LIMIT]");
                break;
            }

            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static int GetEnvInt(string key, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : fallback;
}
