namespace ObjectStorageService
{
    public static class CheckStageParser
    {
        public static bool TryParse(string stage, out CheckStage result)
        {
            switch (stage.ToLowerInvariant())
            {
                case "load": result = CheckStage.Load; return true;
                case "build": result = CheckStage.Build; return true;
                case "reflect": result = CheckStage.Reflect; return true;
                case "llm": result = CheckStage.Llm; return true;
                default: result = default; return false;
            }
        }
    }
}
