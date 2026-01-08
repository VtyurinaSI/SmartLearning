using Serilog.Context;

namespace GatewayPatterns
{
    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (!ctx.Request.Headers.ContainsKey("X-Correlation-Id"))
                ctx.Request.Headers["X-Correlation-Id"] = ctx.TraceIdentifier;

            using (LogContext.PushProperty("CorrelationId", ctx.Request.Headers["X-Correlation-Id"].ToString()))
                await _next(ctx);
        }
    }
}
