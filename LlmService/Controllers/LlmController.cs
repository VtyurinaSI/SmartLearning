using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LlmService.Controllers
{
    #region instructions
    /* консольные запросы:
      $response = Invoke-RestMethod `
          -Method POST `
          -Uri http://localhost:5225/api/llm/chat `
          -ContentType 'application/json' `
         -Body '[{"role":"user","content":"Do you speak russian?"}]'
         $response.content

    запуск:
    .\ollama serve
    */
    #endregion

    [ApiController]
    [Route("api/[controller]")]
    public class LlmController : ControllerBase
    {
        private readonly HttpClient _ollama;

        public LlmController(IHttpClientFactory httpFactory)
        {
            _ollama = httpFactory.CreateClient("Ollama");
        }

        public record ChatMessage(string role, string content);
        public record ChatRequest(string model, ChatMessage[] messages);
        public record Choice(int index, ChatMessage message);
        public record ChatResponse(Choice[] choices);

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatMessage[] messages)
        {
            var request = new ChatRequest(
                model: "qwen2.5-coder:14b",
                messages: messages
            );

            var reqJson = JsonSerializer.Serialize(request);
            var content = new StringContent(reqJson, Encoding.UTF8, "application/json");

            var resp = await _ollama.PostAsync("v1/chat/completions", content);
            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());

            var respJson = await resp.Content.ReadAsStringAsync();
            var chatResp = JsonSerializer.Deserialize<ChatResponse>(respJson);

            return Ok(chatResp?.choices?[0].message);
        }
    }
}
