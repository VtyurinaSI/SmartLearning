using System.Security.Claims;

namespace GatewayPatterns
{
    public sealed class UserIdHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public UserIdHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            ctx.Request.Headers.Remove("X-User-Id");

            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                var uid = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? ctx.User.FindFirstValue("sub");

                if (!string.IsNullOrEmpty(uid))
                    ctx.Request.Headers["X-User-Id"] = uid;
            }
            await _next(ctx);
        }
    }
}
