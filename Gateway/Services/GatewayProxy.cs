using System.Text;

namespace Gateway
{
    public static class GatewayProxy
    {
        public static async Task<IResult> ProxyAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
            var body = await resp.Content.ReadAsStringAsync(ct);
            return Results.Content(body, contentType, Encoding.UTF8, (int)resp.StatusCode);
        }
    }
}

