using System.Threading.RateLimiting;

namespace FocusTrack.Gateway.Api.Middleware;

public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;
    private const string LoginPath = "/signin-oidc";

    public LoginRateLimitMiddleware(RequestDelegate next, PartitionedRateLimiter<HttpContext> limiter)
    {
        _next = next;
        _limiter = limiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(LoginPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        using var lease = await _limiter.AcquireAsync(context, 1, context.RequestAborted);
        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Please try again later." });
            return;
        }

        await _next(context);
    }
}
