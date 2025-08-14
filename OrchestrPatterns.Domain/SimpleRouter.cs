using Microsoft.Extensions.Logging;
using OrchestrPatterns.Domain;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using static System.Net.WebRequestMethods;

public sealed class SimpleRouter(string inputUserData, IHttpClientFactory http, ILogger<SimpleRouter> log)
{
    private const int pause = 1000;
    public record ChatMessage(string role, string content);

    public string? LlmAnswer { get; private set; }
    public async Task HandleAsync(CheckingTrigger trigger, CheckingStatus from, CheckingStatus to,
                                  OrchestrPatterns.Domain.Checking fsm, CancellationToken ct)
    {
        await Task.Delay(pause);
        log.LogDebug("FSM: {From} -> {To}", from, to);
        switch (to)
        {
            case CheckingStatus.Compiling:
                try
                {
                    var inputUserDataToLog = inputUserData.Length < 100 ? inputUserData : inputUserData.Substring(0, 100);
                    log.LogDebug($"Write input user data [{inputUserDataToLog}...] to object storage");
                    //передать метаданные о запросе из object storage, получить новые
                    //await http.CreateClient("compiler")
                    //    .PostAsJsonAsync($"/compile/", new { }, ct);
                    await Task.Delay(pause * 2);
                    fsm.TryFireChecking(CheckingTrigger.CodeCompiled);
                    log.LogDebug("Fired {tr}", trigger);
                    break;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Routing failed for {From} -> {To}", from, to);
                    fsm.TryFireChecking(CheckingTrigger.CompilationFailed);
                    return;
                }
            case CheckingStatus.Compiled:
                fsm.TryFireChecking(CheckingTrigger.StartTests);
                break;
            case CheckingStatus.Testing:
                try
                {
                    log.LogDebug($"Send data to Checking");
                    // передать метаданные о запросе из object storage, получить новые
                    //await http.CreateClient("checker")
                    //          .PostAsJsonAsync($"/check/", new { }, ct);
                    await Task.Delay(pause * 2);
                    fsm.TryFireChecking(CheckingTrigger.TestsFinished);
                    break;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Routing failed for {From} -> {To}", from, to);
                    fsm.TryFireChecking(CheckingTrigger.TestsFailed);
                    return;
                }
            case CheckingStatus.Tested:
                fsm.TryFireChecking(CheckingTrigger.StartReview);
                break;
            case CheckingStatus.Reviewing:

                log.LogDebug($"Send data to LLM");
                await Task.Delay(pause * 2);
                var cm = new ChatMessage[] { new ChatMessage("user", inputUserData) };
                // передать метаданные о запросе из object storage, получить новые
                var resp = await http.CreateClient("reviewer").
                    PostAsJsonAsync("/api/llm/chat", cm, ct);                
                
                if (resp.IsSuccessStatusCode)
                {
                    fsm.TryFireChecking(CheckingTrigger.ReviewFinished);
                    LlmAnswer = await resp.Content.ReadAsStringAsync(ct);
                    break;
                }
                log.LogInformation($"Routing failed for {from} -> {to}");
                fsm.TryFireChecking(CheckingTrigger.ReviewFailed);
                break;
            case CheckingStatus.Created:
                fsm.TryFireChecking(CheckingTrigger.StartReview/*CheckingTrigger.StartCompile*/);
                break;
            case CheckingStatus.Reviewed:
                fsm.TryFireChecking(CheckingTrigger.Finalize);
                break;
            //case CheckingStatus.Passed: 
            default:
                break;

        }

    }
}
