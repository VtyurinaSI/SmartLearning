namespace LlmService
{
    public sealed class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://ollama:11434/";
        public int TimeoutMinutes { get; set; } = 10;
    }
}
