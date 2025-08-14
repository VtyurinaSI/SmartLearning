namespace OrchestrPatterns.Domain
{
    public enum CheckingTrigger
    {        
        StartCompile,
        StartTests,
        StartReview,
        
        CodeCompiled,
        CompilationFailed,
        CompileTimeout,

        TestsFinished,
        TestsFailed,
        TestsTimeout,

        ReviewFinished,
        ReviewFailed,
        ReviewTimeout,
          
        Finalize,     
        Cancel
    }
}
